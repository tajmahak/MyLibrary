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
        LeftOuterJoin,
        RightOuterJoin,
        FullOuterJoin,
        FullOuterJoinAs,
        RightOuterJoinAs,
        LeftOuterJoinAs,
        InnerJoinAs,
        InnerJoin_type,
        LeftOuterJoin_type,
        RightOuterJoin_type,
        FullOuterJoin_type,
        InnerJoinAs_type,
        LeftOuterJoinAs_type,
        RightOuterJoinAs_type,
        FullOuterJoinAs_type,

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
    }
}
