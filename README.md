# Lighting-TriangleMesh

## Abstract
Using 3rd degree Bazier curves program interpolates plane, traingulates and then draws it using [Lambertian light model](https://en.wikipedia.org/wiki/Lambertian_reflectance) and barycentric aproximation.

Mesh is a orthogonal projection (on x-y plane) triangulated 3rd degreee Bazier surface.

<p align="center">
  <img src="https://github.com/macinn/CG-Lighting-TriangleMesh/assets/118574079/3afeb12e-ab4e-4dbf-969a-f28081890278">
</p>

$$z(x,y) = \sum^3_{i=0}\sum^3_{j=0}z_{i,j}B_{i,3}(x)B_{j,3}(y) \quad x,y \in [0,1]$$
$z_{i,j}$ - control points
$$B_{i,n}(t)={n \choose i}t^i(1-t)^{n-i}$$

Filling polygons is programmed using scan-line and vertex sorting. Lighting color is calculated using Lambertian model.

$$ I = k_d \cdot I_L \cdot I_O \cdot \cos(\theta_{N,L}) + k_s \cdot I_L \cdot I_O \cdot \cos^m(\theta_{V,R}) $$
- $k_d$, $k_s$, $m$ - model coefficients
- $I_L$ - light color
- $I_O$ - object color
- $N$ - normal versor
- $L$ - light versor
- $V = [0,0,1]$, $R=2\langle N, L \rangle N - L$

## Availble options: <br>
- Dynamic light postion <br>
- Custom normal map <br>
- Custom texutre <br>
- Adjust object and light color <br>

## Examples:

![example1](https://github.com/macinn/TraingleMesh/assets/118574079/0e99f312-75ba-4e5f-b6d0-a83ef37908e3)
![example2](https://github.com/macinn/TraingleMesh/assets/118574079/439e08c3-e7f4-47ae-88b4-3693386b990b)

