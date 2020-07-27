# Stride Terrain Demo
A custom terrain component and processor for use with the native heightmap asset.

## Usage:
Add terrain component an entity, setup heightmap and material. If using a height field collider then the entity should be offset (0.5, 0, 0.5) from the origin.

## Limitations
* Only support short height conversion type on the heightmap asset, anything else will cause a null reference exception
* Uses a hidden model component, this means that there must be no model component assigned to the entity with the terrain component as only on model component per entity is supported
* No LOD, so probably only suitable for small terrains

## Resources
Textures in Resources/Terrain/Textures are from CC0Textures.com, licensed under the Creative Commons CC0 License.

Heightmap and splat map is created by yours truly.

## License
See License.md

![Screenshot](Screenshot.jpg?raw=true "Screenshot")