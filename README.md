# GPU-boids
Realtime boids simulation (O(n^2)) on GPU in Unity (2018.1.0b8) using HLSL compute shader. 

The boids support the usual flocking behavior (separation, alignment, cohesion), staying in box bounds and maintaining a reasonable speed. Prey boids also seeks the nearest food while fleeing from nearby predators which pursues nearby prey. Boids are also affected by placeable forcefields and drag. Most parameters can be changed from Unity but are not updated while the simulation is running.

All simulation related calculations are performed in the compute shader and the drawing of boids from the simulation information in the surface shader. The C# script is responsible for calling methods to bind and init (set) parameters on the compute shader, run it, bind (set) boids information to the boids material (which uses the surface shader) and draw the boids mesh with material using GPU instancing.

On my system 24 000 boids can be simulated at 30fps, but some exceptions are thrown unrelated to the simulation and caused by the beta version of unity, which hopefully will be fixed in the future.

[![See video](https://img.youtube.com/vi/7eJhNZB6T4M/0.jpg)](https://youtu.be/7eJhNZB6T4M)
