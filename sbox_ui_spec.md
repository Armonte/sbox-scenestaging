# S&Box UI Specification

## Core Architecture

### Component Structure
- S&Box uses Razor components that inherit from `PanelComponent` for root UI elements
- Child elements inherit from `Panel` class (default for .razor files)
- Each component has an accompanying `.razor.scss` file for styling
- Components are scoped - styles don't leak between components

### Panel vs PanelComponent
- **PanelComponent**: Root UI component, added to GameObject with ScreenPanel/WorldPanel
- **Panel**: Child elements within a PanelComponent
- PanelComponent has lifecycle methods (OnStart, OnUpdate, etc.)
- Panel has OnAfterTreeRender(bool firstTime) and Tick()

### Font System
**Primary Fonts Used:**
- `Poppins` - Primary UI font (most common in examples)
- `'Segoe UI', Tahoma, Geneva, Verdana, sans-serif` - Fallback chain
- `'Courier New', monospace` - For timers/monospace elements
- **Note**: Specify single font name, not filename: `font-family: Poppins;`

### Color Patterns
**Text Colors:**
- Primary text: `#fff` or `white`
- Secondary text: `rgba(255, 255, 255, 0.8)` or `rgba(255, 255, 255, 0.9)`
- Role-specific: `#ffcc00` (roles), `#ff6666` (danger)

**Background Colors:**
- Transparent overlays: `rgba(0, 0, 0, 0.8)`
- Button backgrounds: `rgba(255, 255, 255, 0.1)`
- Accent backgrounds: `rgba(255, 200, 0, 0.1)`

### Typography Hierarchy
- **Headers**: 32px-48px
- **Subheaders**: 20px-24px  
- **Body text**: 14px-18px
- **Small text**: 12px-14px

## Styling Architecture

### CSS Selector Pattern
```scss
// CORRECT: Use component name as root selector
MyComponent {
    .child-element {
        property: value;
    }
}

// INCORRECT: Don't use class selectors as root
.my-component {
    // This won't work in S&Box
}
```

### Automatic Stylesheet Loading
- File structure: `Health.razor` + `Health.razor.scss`
- SCSS file automatically included by matching .razor file
- Can specify custom location: `[StyleSheet("main.scss")]`
- Import other stylesheets: `@import "buttons.scss";`

### Layout Patterns
**Common Properties:**
- `display: flex` (default for everything)
- `flex-direction: column` for vertical layouts
- Common gaps: 8px, 10px, 15px, 20px, 30px
- Border-radius: typically 5px-15px
- Margins: Usually 10px-30px

**Flexbox Usage:**
```scss
display: flex;
flex-direction: column;
justify-content: center;
align-items: center;
gap: 15px;
```

## Component Creation Pattern

### Basic Component Setup
```csharp
// MyComponent.razor
@using Sandbox;
@using Sandbox.UI;
@inherits PanelComponent

<root>
    <div class="content">
        <label>@Title</label>
    </div>
</root>

@code {
    [Property] public string Title { get; set; } = "Hello World";
    
    protected override int BuildHash() => System.HashCode.Combine(Title);
}
```

```scss
// MyComponent.razor.scss
MyComponent {
    .content {
        font-family: Poppins;
        color: white;
        font-size: 16px;
    }
}
```

### Integration with Game Objects
```csharp
// In Component class
var uiObject = Scene.CreateObject();
uiObject.Name = "MyHUD";
var screenPanel = uiObject.Components.GetOrCreate<ScreenPanel>();
var panel = uiObject.Components.GetOrCreate<MyPanelComponent>();
```

## Localization System

### Token System
- Strings beginning with `#` are localization tokens
- Looks up values in `Localization/{language}/sandbox.json`
- Example: `<label>#spawnmenu.props</label>`

### Language Files
```json
{
  "menu.helloworld": "Hello World",
  "spawnmenu.props": "Models",
  "spawnmenu.tools": "Tools"
}
```

### Supported Languages
- English (en), French (fr), German (de), Spanish (es)
- Chinese (zh-cn, zh-tw), Japanese (ja), Korean (ko)
- Full list: ar, bg, cs, da, nl, fi, el, hu, it, no, en-pt, pl, pt, pt-br, ro, ru, es-419, sv, th, tr, uk, vn

## HudPainter System

### Direct Drawing Alternative
- More efficient than UI panels for simple graphics
- No layout, stylesheets, or interactivity
- Draw directly in OnUpdate() function

```csharp
protected override void OnUpdate()
{
    if (Scene.Camera is null) return;
    
    var hud = Scene.Camera.Hud;
    hud.DrawRect(new Rect(300, 300, 10, 10), Color.White);
    hud.DrawLine(new Vector2(100, 100), new Vector2(200, 200), 10, Color.White);
    hud.DrawText(new TextRendering.Scope("Hello!", Color.Red, 32), Screen.Width * 0.5f);
}
```

## Advanced Features

### Razor Components
```csharp
// InfoCard.razor component
<root>
    <div class="header">@Header</div>
    <div class="body">@Body</div>
    <div class="footer">@Footer</div>
</root>

@code {
    public RenderFragment Header { get; set; }
    public RenderFragment Body { get; set; }
    public RenderFragment Footer { get; set; }
}

// Usage in another component
<InfoCard>
    <Header>My Card</Header>
    <Body><div class="title">This is my card</div></Body>
    <Footer><div class="button" onclick="@CloseCard">Close</div></Footer>
</InfoCard>
```

### Two-Way Data Binding
```csharp
<SliderEntry min="0" max="100" step="1" Value:bind=@IntValue></SliderEntry>

@code {
    public int IntValue { get; set; } = 32;
}
```

### BuildHash System
- Controls when UI rebuilds
- Only rebuilds if BuildHash() value changes
- Include all data that affects rendering

```csharp
protected override int BuildHash() => System.HashCode.Combine(Health, Armor, PlayerName);
```

## Styling Best Practices

### Text Rendering
```scss
.text-element {
    font-family: Poppins;
    color: rgba(255, 255, 255, 0.8);
    font-weight: normal;
    font-size: 14px;
    line-height: 1.4;
    text-rendering: optimizeLegibility;
}
```

### Button Styling
```scss
.button {
    background: rgba(255, 255, 255, 0.1);
    border: none;
    border-radius: 5px;
    padding: 10px 20px;
    color: white;
    font-family: Poppins;
    cursor: pointer;
    
    &:hover {
        background: rgba(255, 255, 255, 0.2);
    }
}
```

### Custom Properties (S&Box Specific)
- `aspect-ratio: 1;` or `aspect-ratio: 16/9;`
- `background-image-tint: Color` - Multiplies background image
- `text-stroke: Length, Color` - Text outline
- `sound-in: String` - Sound on style application
- `sound-out: String` - Sound on style removal

### Animations & Transitions
```scss
MyPanel {
    transition: all 2s ease-out;
    transform: scale(1);
    
    // Element creation animation
    &:intro {
        transform: scale(0);
    }
    
    // Element deletion animation
    &:outro {
        transform: scale(2);
    }
}
```

## Performance Considerations
- Avoid deep CSS nesting (max 3-4 levels)
- Use scoped styles to prevent conflicts
- Minimize dynamic style changes
- Cache component references when possible
- BuildHash should be efficient and deterministic

## Common Issues and Solutions

### Text Not Styling
1. **Use component name selector**, not class selector
2. **Ensure proper font specification**: `font-family: Poppins;`
3. **Add font-weight**: `font-weight: normal;` for consistency
4. **Check CSS specificity** conflicts
5. **Use inherit**: `font-family: inherit;` to inherit from parent

### Layout Problems
1. **Always specify `display: flex`** when using flexbox properties
2. **Use `flex-direction: column`** for vertical stacking
3. **Add appropriate `gap`** for spacing instead of margins
4. **Check parent container** has proper flex settings

### Font Loading Issues
1. **Stick to established fonts** (`Poppins`, `Segoe UI`)
2. **Provide fallback font stack**
3. **Use `font-family: inherit`** to inherit from parent
4. **Test with simpler fonts** if custom fonts fail

## Debugging UI Issues
1. **Check browser dev tools** for CSS application
2. **Verify component name** matches CSS selector
3. **Test with simplified styles** first
4. **Use background colors** to debug layout issues
5. **Check S&Box console** for CSS parsing errors
6. **Verify BuildHash()** includes all relevant data