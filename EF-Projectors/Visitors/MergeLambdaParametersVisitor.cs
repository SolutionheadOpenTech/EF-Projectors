using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace EF_Projectors.Visitors
{
    internal class MergeLambdaParametersVisitor : ExpressionVisitor
    {
        internal static T MergeLambdaParameters<T>(T lambda, T parametersSource)
            where T : LambdaExpression
        {
            var oldParameters = lambda.Parameters.ToList();
            var newParameters = parametersSource.Parameters.ToList();

            var mapping = oldParameters.Zip(newParameters, (o, n) => new
                {
                    Old = o,
                    New = n
                }).ToDictionary(p => p.Old, p => p.New);

            return (T) new MergeLambdaParametersVisitor(mapping).Visit(lambda);
        }

        private readonly Dictionary<ParameterExpression, ParameterExpression> _parameterReplacements;

        private MergeLambdaParametersVisitor(Dictionary<ParameterExpression, ParameterExpression> parameterReplacements)
        {
            _parameterReplacements = parameterReplacements;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            ParameterExpression replacement;
            if(_parameterReplacements.TryGetValue(node, out replacement))
            {
                node = replacement;
            }
            return base.VisitParameter(node);
        }
    }
}