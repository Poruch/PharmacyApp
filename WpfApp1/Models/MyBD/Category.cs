using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PharmacyApp.Models;

[Table("CATEGORY")]
public class Category : EntityBase
{
    private int _id;
    private string _name = null!;
    private string? _description;
    private int? _parentId;
    private Category? _parentCategory;
    private ICollection<Category>? _childCategories;
    private ICollection<Item>? _items;

    [Key, Column("Id")]
    public int Id
    {
        get => _id;
        set => _id = value;
    }

    [Column("Name"), MaxLength(255)]
    public string Name
    {
        get => _name;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Category name cannot be empty");
            _name = value;
        }
    }

    [Column("Description")]
    public string? Description
    {
        get => _description;
        set => _description = value;
    }

    [Column("ParentId")]
    public int? ParentId
    {
        get => _parentId;
        set => _parentId = value;
    }

    [ForeignKey(nameof(ParentId))]
    public Category? ParentCategory
    {
        get => _parentCategory;
        set => _parentCategory = value;
    }

    public ICollection<Category>? ChildCategories
    {
        get => _childCategories ??= new List<Category>();
        set => _childCategories = value;
    }

    public ICollection<Item>? Items
    {
        get => _items ??= new List<Item>();
        set => _items = value;
    }

    public Category() { }

    public Category(string name, string? description = null, int? parentId = null)
    {
        Name = name;
        Description = description;
        ParentId = parentId;
    }
}