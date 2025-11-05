# Blazor.Icons.Tabler
- possibility to change source for icon-font. Either bake inside or from CDN url
  - maybe as 2 separate packages
    - Blazor.Icons.Tabler
    - Blazor.Icons.Tabler.Local  
- TablerIcon component with parameters
   - Name - iconName 
   - Class
   - Attributes will be applied inside
- pipeline that will every day check official tabler version and if the newer one is out, will automatically update packages with new icons and new iconfont file
