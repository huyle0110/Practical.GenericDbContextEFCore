using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Practical.GenericDbContextEFCore.Extension
{
    public static class ExpressionExtensions
    {
        /// <summary>
        /// combine and distinct multiple expression condition
        /// Should apply for selector
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TDestination"></typeparam>
        /// <param name="selectors"></param>
        /// <returns></returns>
        public static Expression<Func<TSource, TDestination>> Combine<TSource, TDestination>(params Expression<Func<TSource, TDestination>>[] selectors)
        {
            var firstSelectorBody = ((MemberInitExpression)selectors?.FirstOrDefault()?.Body);
            if (firstSelectorBody == null)
                return null;

            var param = selectors[0].Parameters[0];
            List<MemberBinding> bindings = new List<MemberBinding>(firstSelectorBody.Bindings.OfType<MemberAssignment>());
            for (int i = 1; i < selectors.Length; i++)
            {
                var memberInit = (MemberInitExpression)selectors[i].Body;
                var replace = new ParameterReplaceVisitor(selectors[i].Parameters[0], param);
                foreach (var binding in memberInit.Bindings.OfType<MemberAssignment>())
                {
                    bindings.Add(Expression.Bind(binding.Member,
                        replace.VisitAndConvert(binding.Expression, "Combine")));
                }
            }
            bindings = bindings.DistinctBy(x => x.Member.Name).ToList();// distinct member have same condition
            return Expression.Lambda<Func<TSource, TDestination>>(
                Expression.MemberInit(firstSelectorBody.NewExpression, bindings), param);
        }

        private class ParameterReplaceVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _from, _to;
            public ParameterReplaceVisitor(ParameterExpression from, ParameterExpression to)
            {
                _from = from;
                _to = to;
            }

            /// <summary>
            /// VisitParameter
            /// </summary>
            /// <param name="node"></param>
            /// <returns></returns>
            protected override Expression VisitParameter(ParameterExpression node)
            {
                return node == _from ? _to : base.VisitParameter(node);
            }
        }
    }
}
