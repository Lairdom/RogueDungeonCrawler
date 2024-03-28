using Godot;
using System;

public partial class Pathfinding : NavigationRegion3D
{
	Node3D root;
	public Rid smallMap = NavigationServer3D.MapCreate();
	public Rid normalMap = NavigationServer3D.MapCreate();
	public Rid largeMap = NavigationServer3D.MapCreate();
	Rid smallRegion = NavigationServer3D.RegionCreate();
	Rid normalRegion = NavigationServer3D.RegionCreate();
	Rid largeRegion = NavigationServer3D.RegionCreate();
	NavigationMesh smallMesh = new NavigationMesh();
	NavigationMesh normalMesh = new NavigationMesh();
	NavigationMesh largeMesh = new NavigationMesh();
	NavigationMeshSourceGeometryData3D geometry = new NavigationMeshSourceGeometryData3D();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() {
		root = GetNode<Node3D>("/root/World");
		smallMesh.AgentRadius = 0.25f;
		normalMesh.AgentRadius = 0.5f;
		largeMesh.AgentRadius = 0.75f;
		smallMesh.AgentHeight = 1.5f; normalMesh.AgentHeight = 1.5f; largeMesh.AgentHeight = 1.5f;
		NavigationServer3D.ParseSourceGeometryData(smallMesh, geometry, root);
		NavigationServer3D.BakeFromSourceGeometryData(smallMesh, geometry);
		NavigationServer3D.BakeFromSourceGeometryData(normalMesh, geometry);
		NavigationServer3D.BakeFromSourceGeometryData(largeMesh, geometry);
		NavigationServer3D.MapSetActive(smallMap, true);
		NavigationServer3D.MapSetActive(normalMap, true);
		NavigationServer3D.MapSetActive(largeMap, true);
		NavigationServer3D.RegionSetMap(smallRegion, smallMap);
		NavigationServer3D.RegionSetMap(normalRegion, normalMap);
		NavigationServer3D.RegionSetMap(largeRegion, largeMap);
		NavigationServer3D.RegionSetNavigationMesh(smallRegion, smallMesh);
		NavigationServer3D.RegionSetNavigationMesh(normalRegion, normalMesh);
		NavigationServer3D.RegionSetNavigationMesh(largeRegion, largeMesh);
	}
}
