/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

package org.apache.ignite.internal.logger.platform;

import org.apache.ignite.IgniteLogger;
import org.apache.ignite.internal.processors.platform.PlatformNativeException;
import org.apache.ignite.internal.processors.platform.callback.PlatformCallbackGateway;
import org.apache.ignite.internal.util.typedef.X;
import org.jetbrains.annotations.Nullable;

import static org.apache.ignite.IgniteSystemProperties.IGNITE_QUIET;

/**
 * Logger that delegates to platform.
 */
public class PlatformLogger implements IgniteLogger {
    /** */
    private static final int LVL_TRACE = 0;

    /** */
    private static final int LVL_DEBUG = 1;

    /** */
    private static final int LVL_INFO = 2;

    /** */
    private static final int LVL_WARN = 3;

    /** */
    private static final int LVL_ERROR = 4;

    /** Callbacks. */
    private final PlatformCallbackGateway gate;

    /** Quiet flag. */
    private final Boolean quiet;

    /** Category. */
    private final String category;

    /** Trace flag. */
    private final boolean traceEnabled;

    /** Debug flag. */
    private final boolean debugEnabled;

    /** Info flag. */
    private final boolean infoEnabled;

    /**
     * Ctor.
     *
     * @param gate Callback gateway.
     */
    public PlatformLogger(PlatformCallbackGateway gate, Object ctgr) {
        assert gate != null;

        this.gate = gate;

        // Default quiet to false so that Java does not write anything to console.
        // Platform is responsible for console output, we do not want to mix these.
        quiet = Boolean.valueOf(System.getProperty(IGNITE_QUIET, "false"));

        category = getCategoryString(ctgr);

        // Precalculate enabled levels (JNI calls are expensive)
        traceEnabled = gate.loggerIsLevelEnabled(LVL_TRACE);
        debugEnabled = gate.loggerIsLevelEnabled(LVL_DEBUG);
        infoEnabled = gate.loggerIsLevelEnabled(LVL_INFO);
    }

    /**
     * Ctor.
     */
    private PlatformLogger(PlatformCallbackGateway gate, Boolean quiet, String category, boolean traceEnabled,
        boolean debugEnabled, boolean infoEnabled) {
        this.gate = gate;
        this.quiet = quiet;
        this.category = category;
        this.traceEnabled = traceEnabled;
        this.debugEnabled = debugEnabled;
        this.infoEnabled = infoEnabled;
    }

    /** {@inheritDoc} */
    @Override public IgniteLogger getLogger(Object ctgr) {
        return new PlatformLogger(gate, quiet, getCategoryString(ctgr), traceEnabled, debugEnabled, infoEnabled);
    }

    /** {@inheritDoc} */
    @Override public void trace(String msg) {
        log(LVL_TRACE, msg, null);
    }

    /** {@inheritDoc} */
    @Override public void debug(String msg) {
        log(LVL_DEBUG, msg, null);
    }

    /** {@inheritDoc} */
    @Override public void info(String msg) {
        log(LVL_INFO, msg, null);
    }

    /** {@inheritDoc} */
    @Override public void warning(String msg) {
        log(LVL_WARN, msg, null);
    }

    /** {@inheritDoc} */
    @Override public void warning(String msg, @Nullable Throwable e) {
        log(LVL_WARN, msg, e);
    }

    /** {@inheritDoc} */
    @Override public void error(String msg) {
        log(LVL_ERROR, msg, null);
    }

    /** {@inheritDoc} */
    @Override public void error(String msg, @Nullable Throwable e) {
        log(LVL_ERROR, msg, e);
    }

    /** {@inheritDoc} */
    @Override public boolean isTraceEnabled() {
        return traceEnabled;
    }

    /** {@inheritDoc} */
    @Override public boolean isDebugEnabled() {
        return debugEnabled;
    }

    /** {@inheritDoc} */
    @Override public boolean isInfoEnabled() {
        return infoEnabled;
    }

    /** {@inheritDoc} */
    @Override public boolean isQuiet() {
        return quiet;
    }

    /** {@inheritDoc} */
    @Override public String fileName() {
        return null;
    }

    /**
     * Logs the message.
     *
     * @param level Log level.
     * @param msg Message.
     * @param e Exception.
     */
    private void log(int level, String msg, @Nullable Throwable e) {
        String errorInfo = null;

        if (e != null)
            errorInfo = X.getFullStackTrace(e);

        long memPtr = 0;

        PlatformNativeException e0 = X.cause(e, PlatformNativeException.class);

        if (e0 != null) {
            // TODO: Unwrap platform error if possible
            //e0.cause()
        }

        gate.loggerLog(level, msg, category, errorInfo, memPtr);
    }

    /**
     * Gets the category string.
     *
     * @param ctgr Category object.
     * @return Category string.
     */
    private static String getCategoryString(Object ctgr) {
        return ctgr instanceof Class
            ? ((Class)ctgr).getName()
            : (ctgr == null ? null : String.valueOf(ctgr));
    }
}
