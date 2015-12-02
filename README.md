# InkXAMLRenderer
Use Win2D to Render XAML background and overlay ink content from the InkCanvas. The 
InkCanvas control does not support RenderTargetBitmap.RenderAsync() so other methods 
need to be used to render both types of content to a single image. This sample
uses the CanvasGeometry.CreateInk method which does retain the ink highlighter as well.


## License

Microsoft Developer Experience & Evangelism

Copyright (c) Microsoft Corporation. All rights reserved.

THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND, EITHER 
EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY 
AND/OR FITNESS FOR A PARTICULAR PURPOSE.

The example companies, organizations, products, domain names, e-mail addresses, 
logos, people, places, and events depicted herein are fictitious. No association with any 
real company, organization, product, domain name, email address, logo, person, places, or 
events is intended or should be inferred.
