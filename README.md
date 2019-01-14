This project is a remake of the [1D shadow map demo](https://www.gamasutra.com/blogs/RobWare/20180226/313491/Fast_2D_shadows_in_Unity_using_1D_shadow_mapping.php).

![alt tag](https://github.com/LarsIngo/1DShadowMapRemake/blob/master/1DShadowMap.PNG)

Improvements to make:
Percentage Closest Filtering [(CPF)](https://github.com/mattdesl/lwjgl-basics/wiki/2D-Pixel-Perfect-Shadows), a technique commonly used to create soft shadows using a shadow map. Or blur the shadow map horizontally.

Light could be pack into a single matrix, therefore generaing the shadow map in a single draw call.

Depth bias
