namespace MyLibrary.DataBase
{
    public enum DBQueryTypeEnum
    {
        Set,
        Matching,
        Returning,
        Select,
        SelectAs,
        SelectSum,
        SelectSumAs,
        SelectMax,
        SelectMaxAs,
        SelectMin,
        SelectMinAs,
        SelectCount,
        Distinct,
        First,
        Skip,
        Union,

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
        OrderBy,
        OrderByDesc,
        OrderByUpper,
        OrderByUpperDesc,
        GroupBy,
        Select_expression,
        Where_expression,
        Sql,
        OrderBy_expression,
    }
}
