namespace MyLibrary.DataBase
{
    public enum DBQueryTypeEnum
    {
        Set,
        Matching,
        Returning,
        First,
        Skip,
        Distinct,
        Union,
        Sql,

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
        InnerJoin_type,
        LeftOuterJoin_type,
        RightOuterJoin_type,
        FullOuterJoin_type,

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
