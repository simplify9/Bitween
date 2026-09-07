using System;

namespace SW.Bitween.Model;

public class SubscriptionCategoryModel
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedOn { get; set; }
}

public class CreateSubscriptionCategoryModel
{
    /// <summary>Required; the server rejects a create without it.</summary>
    public string Code { get; set; } = null!;

    /// <summary>Optional free text shown beside the code.</summary>
    public string? Description { get; set; }
}

public class SearchSubscriptionCategoryModel
{
    public int? Limit { get; set; }
    public int? Offset { get; set; }
}

public class UpdateSubscriptionCategoryModel : CreateSubscriptionCategoryModel
{
}

public class DeleteSubscriptionCategoryModel
{
}