using Api.Data;
using Api.Dtos;
using Api.Models;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/item-types")]
public class ItemTypesController : ControllerBase
{
    private AppDbContext db;

    public ItemTypesController(AppDbContext db)
    {
        this.db = db;
    }

    [HttpGet]
    public IActionResult GetItemTypes(bool activeOnly = false)
    {
        List<Itemtype> itemTypes = db.Itemtypes.OrderBy(t => t.Name).ToList();

        var result = new List<ItemTypeDto>();
        foreach (Itemtype itemType in itemTypes)
        {
            // skip inactive ones if only active ones were asked for
            if (activeOnly && itemType.Isactive == false)
            {
                continue;
            }

            result.Add(ToDto(itemType));
        }

        return Ok(result);
    }

    [HttpPost]
    public IActionResult CreateItemType(CreateItemTypeRequest request)
    {
        string name = request.Name.Trim();

        string? error = CheckName(name, 0);
        if (error != null)
        {
            return BadRequest(new { detail = error });
        }

        var itemType = new Itemtype();
        itemType.Name = name;
        itemType.Isactive = true;

        db.Itemtypes.Add(itemType);
        db.SaveChanges();

        return StatusCode(201, ToDto(itemType));
    }

    [HttpPut("{id}")]
    public IActionResult UpdateItemType(int id, UpdateItemTypeRequest request)
    {
        Itemtype? itemType = db.Itemtypes.Find(id);
        if (itemType == null)
        {
            return NotFound(new { detail = "Item type with ID " + id + " was not found." });
        }

        string name = request.Name.Trim();

        string? error = CheckName(name, id);
        if (error != null)
        {
            return BadRequest(new { detail = error });
        }

        itemType.Name = name;
        itemType.Isactive = request.IsActive;
        db.SaveChanges();

        return Ok(ToDto(itemType));
    }

    // returns an error message, or null if the name is ok
    // id is the item type being edited (0 when creating a new one)
    private string? CheckName(string name, int id)
    {
        if (name == "")
        {
            return "Name is required.";
        }

        if (name.Length > Itemtype.MaxNameLength)
        {
            return "Name cannot exceed " + Itemtype.MaxNameLength + " characters.";
        }

        bool nameTaken = db.Itemtypes.Any(t => t.Itemtypeid != id && t.Name.ToLower() == name.ToLower());
        if (nameTaken)
        {
            return "An item type named '" + name + "' already exists.";
        }

        return null;
    }

    private ItemTypeDto ToDto(Itemtype itemType)
    {
        return new ItemTypeDto(itemType.Itemtypeid, itemType.Name, itemType.Isactive);
    }
}
