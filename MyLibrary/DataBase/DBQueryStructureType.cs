namespace MyLibrary.DataBase
{
    /// <summary>
    /// Задаёт тип структурного блока <see cref="DBQueryStructureBlock"/> для запроса <see cref="DBQueryBase"/>.
    /// </summary>
    public enum DBQueryStructureType
    {
        Set,
        Matching,
        Returning,
        First,
        Skip,
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
        Select_expression,

        InnerJoin,
        LeftJoin,
        RightJoin,
        FullJoin,
        FullJoinAs,
        RightJoinAs,
        LeftJoinAs,
        InnerJoinAs,
        InnerJoin_type,
        LeftJoin_type,
        RightJoin_type,
        FullJoin_type,
        InnerJoinAs_type,
        LeftJoinAs_type,
        RightJoinAs_type,
        FullJoinAs_type,

        Where,
        WhereBetween,
        WhereUpper,
        WhereContaining,
        WhereContainingUpper,
        WhereLike,
        WhereLikeUpper,
        WhereIn_command,
        WhereIn_values,
        Where_expression,

        OrderBy,
        OrderByDesc,
        OrderByUpper,
        OrderByUpperDesc,
        OrderBy_expression,

        GroupBy,
        GroupBy_expression,
        Having_expression,
    }
}
