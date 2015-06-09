using System.Collections.Generic;
using System.Linq.Expressions;

namespace EF_Projectors.Visitors
{
    internal class ExpressionList : ExpressionVisitor
    {
        internal static List<Expression> BuildList(Expression expression)
        {
            if(expression == null)
            {
                return null;
            }

            var visitor = new ExpressionList();
            visitor.Visit(expression);
            return visitor._expressions;
        }

        private readonly List<Expression> _expressions = new List<Expression>();

        private ExpressionList() { }

        public override Expression Visit(Expression node)
        {
            _expressions.Add(node);
            return base.Visit(node);
        }
    }
}