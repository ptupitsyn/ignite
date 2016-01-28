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

package org.apache.ignite.internal.benchmarks.jmh.runner;

import org.openjdk.jmh.annotations.Mode;
import org.openjdk.jmh.runner.Runner;
import org.openjdk.jmh.runner.options.OptionsBuilder;

import java.util.concurrent.TimeUnit;

/**
 * JMH IDE benchmark runner configuration.
 */
public class JmhIdeBenchmarkRunner {
    /** Benchmark modes. */
    private Mode[] benchmarkModes = new Mode[] { Mode.Throughput };

    /** Amount of forks */
    private int forks = 1;

    /** Warmup iterations. */
    private int warmupIterations = 10;

    /** Measurement operations. */
    private int measurementIterations = 10;

    /** Output time unit. */
    private TimeUnit outputTimeUnit = TimeUnit.SECONDS;

    /** Classes to run. */
    private Class[] clss;

    /** JVM arguments. */
    private String[] jvmArgs;

    /**
     * Create new runner.
     *
     * @return New runner.
     */
    public static JmhIdeBenchmarkRunner create() {
        return new JmhIdeBenchmarkRunner();
    }

    /**
     * Constructor.
     */
    private JmhIdeBenchmarkRunner() {
        // No-op.
    }

    /**
     * @param benchmarkModes Benchmark modes.
     * @return This instance.
     */
    public JmhIdeBenchmarkRunner benchmarkModes(Mode... benchmarkModes) {
        this.benchmarkModes = benchmarkModes;

        return this;
    }

    /**
     * @param forks Forks.
     * @return This instance.
     */
    public JmhIdeBenchmarkRunner forks(int forks) {
        this.forks = forks;

        return this;
    }

    /**
     * @param warmupIterations Warmup iterations.
     * @return This instance.
     */
    public JmhIdeBenchmarkRunner warmupIterations(int warmupIterations) {
        this.warmupIterations = warmupIterations;

        return this;
    }

    /**
     * @param measurementIterations Measurement iterations.
     * @return This instance.
     */
    public JmhIdeBenchmarkRunner measurementIterations(int measurementIterations) {
        this.measurementIterations = measurementIterations;

        return this;
    }
    /**
     * @param outputTimeUnit Output time unit.
     * @return This instance.
     */
    public JmhIdeBenchmarkRunner outputTimeUnit(TimeUnit outputTimeUnit) {
        this.outputTimeUnit = outputTimeUnit;

        return this;
    }

    /**
     * @param clss Classes.
     * @return This instance.
     */
    public JmhIdeBenchmarkRunner classes(Class... clss) {
        this.clss = clss;

        return this;
    }

    /**
     * @param jvmArgs JVM arguments.
     * @return This instance.
     */
    public JmhIdeBenchmarkRunner jvmArguments(String... jvmArgs) {
        this.jvmArgs = jvmArgs;

        return this;
    }

    /**
     * Get prepared options builder.
     *
     * @return Options builder.
     */
    public OptionsBuilder optionsBuilder() {
        OptionsBuilder builder = new OptionsBuilder();

        builder.forks(forks);
        builder.warmupIterations(warmupIterations);
        builder.measurementIterations(measurementIterations);
        builder.timeUnit(outputTimeUnit);

        if (benchmarkModes != null) {
            for (Mode benchmarkMode : benchmarkModes)
                builder.getBenchModes().add(benchmarkMode);
        }

        if (clss != null) {
            for (Class cls : clss)
                builder.include(cls.getSimpleName());
        }

        if (jvmArgs != null)
            builder.jvmArgs(jvmArgs);

        return builder;
    }

    /**
     * Run benchmarks.
     *
     * @throws Exception If failed.
     */
    public void run() throws Exception {
        new Runner(optionsBuilder().build()).run();
    }

    /**
     * Create property.
     *
     * @param name Name.
     * @param val Value.
     * @return Result.
     */
    public static String createProperty(String name, Object val) {
        return "-D" + name + "=" + val;
    }
}
