using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EF_Projectors.Visitors
{
    internal class ExpressionMergeVisitor : ExpressionVisitor
    {
        internal static T Merge<T>(params T[] expressions)
            where T : Expression
        {
            if(expressions == null)
            {
                return null;
            }

            return PrivateMerge(expressions.Reverse().ToArray());
        }

        private static T PrivateMerge<T>(params T[] expressions)
            where T : Expression
        {
            var list = expressions == null ? new List<T>() : expressions.ToList();
            if(!list.Any())
            {
                return null;
            }

            var rootExpression = list.First();
            list.Remove(rootExpression);

            if(!list.Any())
            {
                return rootExpression;
            }

            var merged = new ExpressionMergeVisitor(list.Select(ExpressionList.BuildList)).Visit(rootExpression);
            return (T)merged;
        }

        private readonly List<List<Expression>> _otherExpressions;
        private int _currentDepth;

        private ExpressionMergeVisitor(IEnumerable<List<Expression>> otherExpressions)
        {
            _otherExpressions = otherExpressions.ToList();
        }

        public override Expression Visit(Expression node)
        {
            _otherExpressions.RemoveAll(l =>
            {
                if(l.Count <= _currentDepth)
                {
                    return true;
                }
                if(node == null)
                {
                    return l[_currentDepth] != null;
                }
                return l[_currentDepth].Type != node.Type;
            });
            _currentDepth += 1;
            return base.Visit(node);
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            var previousIndex = _currentDepth - 1;
            _otherExpressions.RemoveAll(l =>
            {
                if(l.Count <= previousIndex)
                {
                    return true;
                }

                var other = l[previousIndex] as Expression<T>;
                if(other == null)
                {
                    return true;
                }

                other = MergeLambdaParametersVisitor.MergeLambdaParameters(other, node);
                l.RemoveRange(previousIndex, l.Count - previousIndex);
                l.AddRange(ExpressionList.BuildList(other));
                return false;
            });
            return base.VisitLambda(node);
        }

        protected override Expression VisitMemberInit(MemberInitExpression node)
        {
            var previousIndex = _currentDepth - 1;
            _otherExpressions.RemoveAll(l =>
            {
                var other = l[previousIndex] as MemberInitExpression;
                if(other != null)
                {
                    node = MergeMemberInit(node, other);
                }
                return true;
            });
            return node;
        }

        private static MemberInitExpression MergeMemberInit(MemberInitExpression original, MemberInitExpression other)
        {
            var otherAssignments = other.Bindings.OfType<MemberAssignment>().ToList();
            var newBindings = new List<MemberBinding>();
            foreach(var assignment in original.Bindings.OfType<MemberAssignment>())
            {
                var newAssignment = assignment;
                var otherAssignment = otherAssignments.FirstOrDefault(b => b.Member == assignment.Member);
                if(otherAssignment != null)
                {
                    otherAssignments.Remove(otherAssignment);
                    newAssignment = Expression.Bind(assignment.Member, PrivateMerge(assignment.Expression, otherAssignment.Expression));
                }
                newBindings.Add(newAssignment);
            }
            newBindings.AddRange(otherAssignments);

            return Expression.MemberInit(original.NewExpression, newBindings);
        }
    }
}