using Microsoft.VisualStudio.TestTools.UnitTesting;
using Vivarium.World;

namespace Vivarium.Tests.World;

[TestClass]
public class GridCellTests
{
	[TestMethod]
	public void GridCell_IsReadOnlyStruct()
	{
		var type = typeof(GridCell);
		
		Assert.IsNotNull(type);
		Assert.IsTrue(type.IsValueType);
		Assert.IsTrue(type.IsSealed);
	}

	[TestMethod]
	public void GridCell_ImplementsIEquatable()
	{
		var type = typeof(GridCell);
		var interfaces = type.GetInterfaces();
		
		Assert.IsTrue(Array.Exists(interfaces, i => i.Name == "IEquatable`1"));
	}

	[TestMethod]
	public void GridCell_Properties_Exist()
	{
		var type = typeof(GridCell);
		
		var typeProp = type.GetProperty("Type");
		var indexProp = type.GetProperty("Index");
		
		Assert.IsNotNull(typeProp);
		Assert.IsNotNull(indexProp);
		
		Assert.AreEqual(typeof(EntityType), typeProp.PropertyType);
		Assert.AreEqual(typeof(int), indexProp.PropertyType);
	}

	[TestMethod]
	public void GridCell_Empty_ReturnsEmptyCell()
	{
		var empty = GridCell.Empty;
		
		Assert.AreEqual(EntityType.Empty, empty.Type);
		Assert.AreEqual(0, empty.Index);
	}

	[TestMethod]
	public void GridCell_CanBeConstructed()
	{
		var cell = new GridCell(EntityType.Agent, 5);
		
		Assert.AreEqual(EntityType.Agent, cell.Type);
		Assert.AreEqual(5, cell.Index);
	}

	[TestMethod]
	public void GridCell_EqualityOperators_Work()
	{
		var cell1 = new GridCell(EntityType.Agent, 5);
		var cell2 = new GridCell(EntityType.Agent, 5);
		var cell3 = new GridCell(EntityType.Plant, 5);
		
		Assert.IsTrue(cell1 == cell2);
		Assert.IsFalse(cell1 != cell2);
		Assert.IsTrue(cell1 != cell3);
	}

	[TestMethod]
	public void GridCell_EmptyEquality_Works()
	{
		var empty1 = GridCell.Empty;
		var empty2 = new GridCell(EntityType.Empty, 0);
		
		Assert.IsTrue(empty1 == empty2);
	}

	[TestMethod]
	public void GridCell_Equals_Method_Works()
	{
		var cell1 = new GridCell(EntityType.Agent, 5);
		var cell2 = new GridCell(EntityType.Agent, 5);
		
		Assert.IsTrue(cell1.Equals(cell2));
		Assert.IsFalse(cell1.Equals(new GridCell(EntityType.Plant, 5)));
	}

	[TestMethod]
	public void GridCell_GetHashCode_IsConsistent()
	{
		var cell1 = new GridCell(EntityType.Agent, 5);
		var cell2 = new GridCell(EntityType.Agent, 5);
		
		Assert.AreEqual(cell1.GetHashCode(), cell2.GetHashCode());
	}
}
