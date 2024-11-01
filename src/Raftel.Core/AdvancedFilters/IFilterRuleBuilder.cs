using System.Linq.Expressions;
using Raftel.Shared.Common;

namespace Raftel.Core.AdvancedFilters;

public interface IFilterRuleBuilder<TModel>
{
    IFilterRuleBuilder<TModel> StartsWith(Expression<Func<TModel, object>> expression, string value);
    IFilterRuleBuilder<TModel> NotStartsWith(Expression<Func<TModel, object>> expression, string value);
    IFilterRuleBuilder<TModel> EndsWith(Expression<Func<TModel, object>> expression, string value);
    IFilterRuleBuilder<TModel> NotEndsWith(Expression<Func<TModel, object>> expression, string value);
    IFilterRuleBuilder<TModel> Contains(Expression<Func<TModel, object>> expression, string value);
    IFilterRuleBuilder<TModel> NotContains(Expression<Func<TModel, object>> expression, string value);
    IFilterRuleBuilder<TModel> Equal(Expression<Func<TModel, object>> expression, object value);
    IFilterRuleBuilder<TModel> NotEqual(Expression<Func<TModel, object>> expression, object value);
    IFilterRuleBuilder<TModel> In(Expression<Func<TModel, object>> expression, dynamic values);
    IFilterRuleBuilder<TModel> NotIn(Expression<Func<TModel, object>> expression, dynamic values);
    IFilterRuleBuilder<TModel> Empty(Expression<Func<TModel, object>> expression);
    IFilterRuleBuilder<TModel> NotEmpty(Expression<Func<TModel, object>> expression);
    IFilterRuleBuilder<TModel> Null(Expression<Func<TModel, object>> expression);
    IFilterRuleBuilder<TModel> NotNull(Expression<Func<TModel, object>> expression);
    IFilterRuleBuilder<TModel> GreaterThan(Expression<Func<TModel, object>> expression, dynamic value);
    IFilterRuleBuilder<TModel> GreaterThanOrEqual(Expression<Func<TModel, object>> expression, dynamic value);
    IFilterRuleBuilder<TModel> Between<TRangeType>(Expression<Func<TModel, object>> expression, Range<TRangeType> range)
        where TRangeType : struct, IComparable<TRangeType>;
    IFilterRuleBuilder<TModel> NotBetween<TRangeType>(Expression<Func<TModel, object>> expression, Range<TRangeType> range)
        where TRangeType : struct, IComparable<TRangeType>;
 
    IFilterRuleBuilder<TModel> And(
        Expression<Func<IFilterRuleBuilder<TModel>, IFilterRuleBuilder<TModel>>> filterExpression);

    IFilterRuleBuilder<TModel> Or(
        Expression<Func<IFilterRuleBuilder<TModel>, IFilterRuleBuilder<TModel>>> filterExpression);
}