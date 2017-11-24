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

package org.apache.ignite.internal.processors.platform.client.cache;

import org.apache.ignite.cache.query.FieldsQueryCursor;
import org.apache.ignite.cache.query.Query;
import org.apache.ignite.cache.query.SqlFieldsQuery;
import org.apache.ignite.cache.query.SqlQuery;
import org.apache.ignite.internal.binary.BinaryRawReaderEx;
import org.apache.ignite.internal.processors.cache.DynamicCacheDescriptor;
import org.apache.ignite.internal.processors.cache.query.SqlFieldsQueryEx;
import org.apache.ignite.internal.processors.odbc.jdbc.JdbcStatementType;
import org.apache.ignite.internal.processors.platform.cache.PlatformCache;
import org.apache.ignite.internal.processors.platform.client.ClientConnectionContext;
import org.apache.ignite.internal.processors.platform.client.ClientResponse;
import org.apache.ignite.internal.processors.query.QueryUtils;

import java.util.List;
import java.util.concurrent.TimeUnit;

/**
 * Sql query request.
 */
@SuppressWarnings("unchecked")
public class ClientCacheSqlFieldsQueryRequest extends ClientCacheRequest {
    /** Query. */
    private final Query qry;

    /** Include field names flag. */
    private final boolean includeFieldNames;

    /**
     * Whether this query has full syntax (select ... from ...) (when true)
     * or simplified syntax (where ...) (when false).
     * Simplified syntax always returns full cache entries (BinaryObject key + BinaryObject val).
     * Full syntax returns a number of fields depending on the query.
     */
    private final boolean isFieldsQuery;

    /**
     * Ctor.
     *
     * @param reader Reader.
     */
    public ClientCacheSqlFieldsQueryRequest(BinaryRawReaderEx reader) {
        super(reader);

        // Same request format as in JdbcQueryExecuteRequest.
        String schema = reader.readString();
        int pageSize = reader.readInt();
        reader.readInt();  // maxRows
        String sql = reader.readString();
        Object[] args = PlatformCache.readQueryArgs(reader);
        JdbcStatementType stmtType = JdbcStatementType.fromOrdinal(reader.readByte());
        boolean distributedJoins = reader.readBoolean();
        boolean loc = reader.readBoolean();
        boolean replicatedOnly = reader.readBoolean();
        boolean enforceJoinOrder = reader.readBoolean();
        boolean collocated = reader.readBoolean();
        boolean lazy = reader.readBoolean();
        int timeout = (int) reader.readLong();

        isFieldsQuery = reader.readBoolean();
        includeFieldNames = reader.readBoolean();

        if (isFieldsQuery) {
            SqlFieldsQuery qry = stmtType == JdbcStatementType.ANY_STATEMENT_TYPE
                    ? new SqlFieldsQuery(sql)
                    : new SqlFieldsQueryEx(sql, stmtType == JdbcStatementType.SELECT_STATEMENT_TYPE);

            qry.setSchema(schema)
                    .setPageSize(pageSize)
                    .setArgs(args)
                    .setDistributedJoins(distributedJoins)
                    .setLocal(loc)
                    .setReplicatedOnly(replicatedOnly)
                    .setEnforceJoinOrder(enforceJoinOrder)
                    .setCollocated(collocated)
                    .setLazy(lazy)
                    .setTimeout(timeout, TimeUnit.MILLISECONDS);

            this.qry = qry;
        } else {
            // TODO: validate invalid options, like stmtType
            SqlQuery qry = new SqlQuery(sql)

        }
    }

    /** {@inheritDoc} */
    @Override public ClientResponse process(ClientConnectionContext ctx) {
        ctx.incrementCursors();

        try {
            if (qry instanceof SqlFieldsQuery) {
                SqlFieldsQuery qry0 = (SqlFieldsQuery)qry;

                // If cacheId is provided, we must check the cache for existence.
                if (cacheId() != 0) {
                    DynamicCacheDescriptor desc = cacheDescriptor(ctx);

                    if (qry0.getSchema() == null) {
                        String schema = QueryUtils.normalizeSchemaName(desc.cacheName(),
                                desc.cacheConfiguration().getSqlSchema());

                        qry0.setSchema(schema);
                    }
                }

                List<FieldsQueryCursor<List<?>>> curs = ctx.kernalContext().query()
                        .querySqlFieldsNoCache(qry0, true, true);

                assert curs.size() == 1;

                FieldsQueryCursor cur = curs.get(0);

                ClientCacheFieldsQueryCursor cliCur = new ClientCacheFieldsQueryCursor(
                        cur, qry0.getPageSize(), ctx);

                long cursorId = ctx.resources().put(cliCur);

                cliCur.id(cursorId);

                return new ClientCacheSqlFieldsQueryResponse(requestId(), cliCur, cur, includeFieldNames);
            } else {
                // TODO
            }
        }
        catch (Exception e) {
            ctx.decrementCursors();

            throw e;
        }
    }
}
