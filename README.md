# Stride Terrain Demo
Implementes a terrain renderer as well as some associated features.

## Features
* Terrain rendering component
* Vegetation scattering support using instancing
* A spline renderer that can optinally be aligned to the height map (for roads etc)

## Terrain Usage:
Add terrain component an entity, setup heightmap and material. If using a height field collider then the entity should be offset (0.5, 0, 0.5) from the origin.

## Spline
Add spline component, then add child entities, these children will be used as control points for the spline. Terrain can be set on the spline component if it should be aligned to the terrain heightmap.

## Limitations and issues
* Only support short height conversion type on the heightmap asset, anything else will cause a null reference exception
* Uses a hidden model component, this means that there must be no model component assigned to the entity with the terrain component as only on model component per entity is supported
* No LOD, so probably only suitable for small terrains
* Exception when loading in Game Studio, just press Resume until it goes away

## Resources
Textures in Resources/Terrain/Textures are from CC0Textures.com, licensed under the Creative Commons CC0 License.

Heightmap and splat map is created by yours truly.

## License
See License.md

![Screenshot](Screenshot.jpg?raw=true "Screenshot")