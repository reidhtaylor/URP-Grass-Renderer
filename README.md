# Scorpion studios - Unity URP Grass Renderer - Compute Shaders

• Compatible with iOS/MacOS & Android/PC

<div>
  <img src="https://user-images.githubusercontent.com/60525644/185021291-f69ac80d-17f6-45ad-9a7b-76d7a4710f1b.jpg" width="300">
  <img src="https://user-images.githubusercontent.com/60525644/185021236-de131947-5310-40d0-8047-f03915d72c2e.jpg" width="300">
  <img src="https://user-images.githubusercontent.com/60525644/185021646-60bed531-8c81-489b-8bfc-3405f4fe0ecf.jpg" width="300">
  <img src="https://user-images.githubusercontent.com/60525644/185021654-15a02809-86e5-4845-b56d-b29c7b57bcec.jpg" width="300">
</div>

## Compatibility

I needed a Grass System that could run on Metal 2... 

So geometry shaders were not an option! After some deep diving into tutorials/articles on Compute Shaders/Compute Buffers and lots of refinement, I created this Renderer. 

From my tests, it works great on MacOS, iOS, Android, and (Obviously) PCs. A large amount of grass (probably about 300k) runs at 60fps on my Mac Laptop.

## USE:
• Paint & Erase Grass with Simple Editor Brush and Controls

• Create Custom Materials for uniqe looks and different reactions to light

• Adjust the shape with many simple parameters like Height, Width, Bend, Variation, Detail, ect...

• Add unlimited 'Tramplers' to make trails/flatten in the grass (like a character walking will flatten the grass)

• Adjust Wind values for a strong storm or light breeze


https://user-images.githubusercontent.com/60525644/185022770-e4029248-a5cf-4a26-a53e-fc79f129c678.mov

