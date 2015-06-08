using System;
using System.Linq.Expressions;

namespace EF_Projectors.Visitors
{
    internal class GetFirstMemberInitLambdaVisitor : ExpressionVisitor
    {
        internal static LambdaExpression Get(Type initType, Expression expression)
        {
            var visitor = new GetFirstMemberInitLambdaVisitor(initType);
            visitor.Visit(expression);
            return visitor._lambdaExpression;
        }

        private GetFirstMemberInitLambdaVisitor(Type initType)
        {
            _initType = initType;
        }

        private readonly Type _initType;
        private LambdaExpression _lambdaExpression;

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            if(_lambdaExpression == null)
            {
                var memberInit = node.Body as MemberInitExpression;
                if(memberInit != null && (_initType == null || memberInit.Type == _initType))
                {
                    _lambdaExpression = node;
                    return node;
                }
            }

            return base.VisitLambda<T>(node);
        }
    }

    internal class GetFirstMemberInitVisitor : ExpressionVisitor
    {
        internal static Expression Get(Type initType, Expression expression)
        {
            var visitor = new GetFirstMemberInitVisitor(initType);
            visitor.Visit(expression);
            return visitor._memberInitExpression;
        }

        private GetFirstMemberInitVisitor(Type initType)
        {
            _initType = initType;
        }

        private readonly Type _initType;
        private Expression _memberInitExpression;

        public override Expression Visit(Expression node)
        {
            if(_memberInitExpression == null)
            {
                var memberInit = node as MemberInitExpression;
                if(memberInit != null && (_initType == null || memberInit.Type == _initType))
                {
                    _memberInitExpression = node;
                    return node;
                }
            }

            return base.Visit(node);
        }
    }
}