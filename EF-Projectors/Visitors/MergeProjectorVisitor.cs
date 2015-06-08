using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EF_Projectors.Visitors
{
    internal class MergeOrReplaceVisitor : ExpressionVisitor
    {
        internal static T MergeOrReplace<T>(params T[] expressions)
            where T : Expression
        {
            if(expressions == null)
            {
                return null;
            }
            
            T rootExpression = null;
            Expression targetExpression = null;
            LambdaExpression targetProjector = null;
            var otherExpressions = new List<Expression>();
            foreach(var expression in expressions)
            {
                if(rootExpression == null)
                {
                    rootExpression = expression;
                    targetProjector = GetFirstMemberInitLambdaVisitor.Get(null, rootExpression);
                    targetExpression = targetProjector != null ? targetProjector.Body : rootExpression;
                }
                else
                {
                    Expression otherExpression = null;
                    if(targetProjector != null)
                    {
                        var otherProjector = GetFirstMemberInitLambdaVisitor.Get(targetExpression.Type, expression);
                        if(otherProjector != null)
                        {
                            otherExpression = MergeLambdaParametersVisitor.MergeLambdaParameters(otherProjector, targetProjector).Body;
                        }
                    }
                    else
                    {
                        otherExpression = GetFirstMemberInitVisitor.Get(targetExpression.Type, expression);
                    }

                    if(otherExpression != null)
                    {
                        otherExpressions.Add(otherExpression);
                    }
                }
            }

            if(!otherExpressions.Any())
            {
                return expressions.LastOrDefault();
            }

            return (T) new MergeOrReplaceVisitor(targetExpression, otherExpressions).Visit(rootExpression);
        }

        private readonly Expression _firstProjector;
        private readonly List<Expression> _otherProjectors;

        private MergeOrReplaceVisitor(Expression firstProjector, List<Expression> otherProjectors)
        {
            _firstProjector = firstProjector;
            _otherProjectors = otherProjectors;
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            if(node == _firstProjector)
            {
                var merged = _otherProjectors.Aggregate((MemberInitExpression)_firstProjector, (c, o) => MergeMemberInits(c, (MemberInitExpression)o));
                return merged;
            }
            return base.VisitMemberInit(node);
        }

        private static MemberInitExpression MergeMemberInits(MemberInitExpression source, MemberInitExpression other)
        {
            var originalBindings = source.Bindings.ToList();
            var otherBindings = other.Bindings.ToDictionary(b => b.Member, b => b);
            var newBindings = new List<MemberBinding>();

            foreach(var oldBinding in originalBindings)
            {
                Expression mergedAssignment = null;
                var oldAssignment = oldBinding as MemberAssignment;
                if(oldAssignment != null)
                {
                    MemberBinding otherBinding;
                    if(otherBindings.TryGetValue(oldBinding.Member, out otherBinding))
                    {
                        var otherAssignment = otherBinding as MemberAssignment;
                        if(otherAssignment != null)
                        {
                            otherBindings.Remove(oldAssignment.Member);
                            mergedAssignment = MergeOrReplace(oldAssignment.Expression, otherAssignment.Expression);
                            newBindings.Add(Expression.Bind(oldAssignment.Member, mergedAssignment));
                        }
                    }
                }

                if(mergedAssignment == null)
                {
                    newBindings.Add(oldBinding);
                }
            }

            newBindings.AddRange(otherBindings.Values);
            return Expression.MemberInit(source.NewExpression, newBindings);
        }
    }
}