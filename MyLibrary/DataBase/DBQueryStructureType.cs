namespace MyLibrary.DataBase
{
    /// <summary>
    /// Задаёт тип структурного блока <see cref="DBQueryStructureBlock"/> для запроса <see cref="DBQueryBase"/>.
    /// </summary>
    public enum DBQueryStructureType
    {
        UpdateOrInsert,

        Set,
        Returning,
        Limit,
        Offset,
        Distinct,
        UnionAll,
        UnionDistinct,

        Select,
        SelectAs,
        SelectSum,
        SelectSumAs,
        SelectMax,
        SelectMaxAs,
        SelectMin,
        SelectMinAs,
        SelectCount,
        SelectExpression,

        Join,
        InnerJoin,
        LeftJoin,
        RightJoin,
        FullJoin,
        FullJoinAs,
        RightJoinAs,
        LeftJoinAs,
        InnerJoinAs,
        InnerJoinType,
        LeftJoinType,
        RightJoinType,
        FullJoinType,
        InnerJoinAsType,
        LeftJoinAsType,
        RightJoinAsType,
        FullJoinAsType,

        Where,
        WhereBetween,
        WhereUpper,
        WhereContaining,
        WhereContainingUpper,
        WhereLike,
        WhereLikeUpper,
        WhereInQuery,
        WhereInValues,
        WhereExpression,

        OrderBy,
        OrderByDescending,
        OrderByExpression,
        OrderByDescendingExpression,
        OrderByUpper,
        OrderByUpperDesc,

        GroupBy,
        GroupByExpression,
        HavingExpression,
    }
}
