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

namespace Apache.Ignite.Core.Impl.Compute.Extensions
{
    using System;
    using System.Collections;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Runtime.Serialization;
    using Apache.Ignite.Core.Compute;
    using Apache.Ignite.Core.Impl.Common;

    /// <summary>
    /// Compute func from a delegate.
    /// </summary>
    [Serializable]
    internal class ComputeDelegateFunc<TRes> : SerializableWrapper<Func<TRes>>, IComputeFunc<TRes>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComputeDelegateFunc{R}"/> class.
        /// </summary>
        /// <param name="func">The function to wrap.</param>
        public ComputeDelegateFunc(Func<TRes> func) : base(func)
        {
            // No-op.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComputeDelegateFunc{R}"/> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="context">The context.</param>
        public ComputeDelegateFunc(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            // No-op.
        }

        /** <inheritdoc /> */
        public TRes Invoke()
        {
            return WrappedObject();
        }
    }

    /// <summary>
    /// Compute func from a delegate.
    /// </summary>
    internal class ComputeDelegateFunc2<TRes> : IComputeFunc<TRes>, IComputeOutFunc
    {
        /** */
        private readonly Expression _body; 

        /** */
        private readonly ICollection _params;

        // MetaLinq https://github.com/mcintyre321/MetaLinq
        // http://expressiontree.codeplex.com/

        // TODO: OR don't use [Serializable] and rely on dynamic registration instead, damn it.

        // TODO: Or use SerializableWrapper to avoid dynamic registration, if it can handle the parameter arrays

        // TODO: Implement IComputeOutFunc here and avoid registration this way?

        /// <summary>
        /// Initializes a new instance of the <see cref="ComputeDelegateFunc{R}"/> class.
        /// </summary>
        /// <param name="expr">The function to wrap.</param>
        public ComputeDelegateFunc2(Expression<Func<TRes>> expr)
        {
            _body = expr.Body;
            _params = expr.Parameters;
        }


        /** <inheritdoc /> */
        TRes IComputeFunc<TRes>.Invoke()
        {
            var e = Expression.Lambda<Func<TRes>>(_body, _params.OfType<ParameterExpression>());

            return e.Compile()();
        }

        object IComputeFunc<object>.Invoke()
        {
            var e = Expression.Lambda<Func<TRes>>(_body, _params.OfType<ParameterExpression>());

            return e.Compile()();
        }
    }

    /// <summary>
    /// Compute func from a delegate.
    /// </summary>
    [Serializable]
    internal class ComputeDelegateFunc<TArg, TRes> : SerializableWrapper<Func<TArg, TRes>>, IComputeFunc<TArg, TRes>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ComputeDelegateFunc{R}"/> class.
        /// </summary>
        /// <param name="func">The function to wrap.</param>
        public ComputeDelegateFunc(Func<TArg, TRes> func) : base(func)
        {
            // No-op.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComputeDelegateFunc{R}"/> class.
        /// </summary>
        /// <param name="info">The information.</param>
        /// <param name="context">The context.</param>
        public ComputeDelegateFunc(SerializationInfo info, StreamingContext context): base(info, context)
        {
            // No-op.
        }

        /** <inheritdoc /> */
        public TRes Invoke(TArg arg)
        {
            return WrappedObject(arg);
        }
    }
}
