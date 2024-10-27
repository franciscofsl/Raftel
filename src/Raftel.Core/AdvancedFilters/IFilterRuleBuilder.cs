using System.Linq.Expressions;

namespace Raftel.Core.AdvancedFilters;

public interface IFilterRuleBuilder<TModel>
{
    IFilterRuleBuilder<TModel> StartsWith(Expression<Func<TModel, object>> expression, string value);
    IFilterRuleBuilder<TModel> NotStartsWith(Expression<Func<TModel, object>> expression, string value);
    IFilterRuleBuilder<TModel> EndsWith(Expression<Func<TModel, object>> expression, string value);
    IFilterRuleBuilder<TModel> NotEndsWith(Expression<Func<TModel, object>> expression, string value);
    IFilterRuleBuilder<TModel> Contains(Expression<Func<TModel, object>> expression, string value);
    IFilterRuleBuilder<TModel> NotContains(Expression<Func<TModel, object>> expression, string value);
    IFilterRuleBuilder<TModel> Equal(Expression<Func<TModel, object>> expression, string value);
    IFilterRuleBuilder<TModel> NotEqual(Expression<Func<TModel, object>> expression, string value);
    IFilterRuleBuilder<TModel> In(Expression<Func<TModel, object>> expression, string[] values);
    IFilterRuleBuilder<TModel> NotIn(Expression<Func<TModel, object>> expression, string[] values);
    IFilterRuleBuilder<TModel> Empty(Expression<Func<TModel, object>> expression);
    IFilterRuleBuilder<TModel> NotEmpty(Expression<Func<TModel, object>> expression);
    IFilterRuleBuilder<TModel> Null(Expression<Func<TModel, object>> expression);
    IFilterRuleBuilder<TModel> NotNull(Expression<Func<TModel, object>> expression);

    IFilterRuleBuilder<TModel> And(
        Expression<Func<IAdvancedFilter<TModel>, IAdvancedFilter<TModel>>> filterExpression);

    IFilterRuleBuilder<TModel> Or(
        Expression<Func<IAdvancedFilter<TModel>, IAdvancedFilter<TModel>>> filterExpression);

    List<Rule> GetRulesWithCondition(Condition condition);
}