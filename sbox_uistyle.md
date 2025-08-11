# S&Box UI Style Properties Reference

## Common Data Types

| Name | Description | Examples |
|------|-------------|----------|
| `Float` | A standard float | `flex-grow: 2.5;` |
| `String` | A string with or without quotes | `font-family: Poppins;`, `content: "Back to Menu";` |
| `int` | A standard int | `font-weight: 600;` |
| `Color` | Can have alpha | `color: #fff;`, `color: #ffffffaa;`, `color: rgba(red, 0.5);` |
| `Length` | A dimension, pixel or relative | `left: 10px;`, `left: 10%;`, `left: 10em;`, `left: 10vw;`, `mask-angle: 10deg;` |

## Custom Style Properties (S&Box Specific)

| Property | Parameters | Description/Examples |
|----------|------------|---------------------|
| `aspect-ratio` | Float, Float (Optional) | `aspect-ratio: 1;`, `aspect-ratio: 16/9;` |
| `background-image-tint` | Color | Multiplies the background-image by this Color. Not a replacement for filter or backdrop-filter. |
| `border-image-tint` | Color | Multiplies the border-image by this Color. |
| `mask-scope` | default / filter | default will apply the mask normally, filter will use the mask to blend between unfiltered and filtered. |
| `sound-in` | String | The name of the sound to play when this style is applied to an element. Useful for :hover or :active styles |
| `sound-out` | String | The name of a sound to play when this style is removed from an element. |
| `text-stroke` | Length, Color | This will put an outline around text |
| `text-stroke-color` | Color | Color of text stroke |
| `text-stroke-width` | Length | Width of text stroke |

## Layout & Positioning

| Property | Values | Examples/Notes |
|----------|--------|----------------|
| `align-content` | auto, flex-end, flex-start, center, stretch, space-between, space-around, baseline | |
| `align-items` | Same as align-content | |
| `align-self` | Same as align-content | |
| `display`* | flex (default), none | Everything is flex by default |
| `flex-basis` | Length | |
| `flex-direction` | row (default), row-reverse, column, column-reverse | |
| `flex-grow` | Float | |
| `flex-shrink` | Float | |
| `flex-wrap` | nowrap (default), wrap, wrap-reverse | |
| `gap` | Length, Length (optional) | Shorthand for row-gap and column-gap |
| `column-gap` | Length | |
| `row-gap` | Length | |
| `justify-content` | Same as align-content | |
| `order` | int | |
| `position`* | static (default), relative, absolute | See: https://yogalayout.com/docs/absolute-relative-layout/ |

## Dimensions & Spacing

| Property | Values | Examples/Notes |
|----------|--------|----------------|
| `width` | Length | |
| `height` | Length | |
| `min-width` | Length | |
| `min-height` | Length | |
| `max-width` | Length | |
| `max-height` | Length | |
| `left` | Length | |
| `right` | Length | |
| `top` | Length | |
| `bottom` | Length | |
| `margin` | Length | Fills in margin-top, margin-right, margin-bottom, margin-left |
| `margin-top` | Length | |
| `margin-right` | Length | |
| `margin-bottom` | Length | |
| `margin-left` | Length | |
| `padding` | Length | Fills in padding-top, padding-right, padding-bottom, padding-left |
| `padding-top` | Length | |
| `padding-right` | Length | |
| `padding-bottom` | Length | |
| `padding-left` | Length | |

## Typography

| Property | Values | Examples/Notes |
|----------|--------|----------------|
| `color` | Color, linear-gradient(Color, Color), radial-gradient(Color, Color), conic-gradient(Color, Color) | |
| `font-family`* | String | Specify single font name, not filename: `font-family: Comic Sans MS;` |
| `font-size` | Length | |
| `font-style` | normal (default), italic | |
| `font-weight` | normal (default), bold, light, bolder, lighter, black, int | `font-weight: bold;`, `font-weight: 300;` |
| `font-color` | Color | |
| `font-smooth` | auto, always, never | never is good for pixel fonts |
| `line-height` | Length | |
| `letter-spacing` | Length, normal | |
| `word-spacing` | Length | |
| `text-align` | left (default), center, right | |
| `text-transform` | none (default), capitalize, lowercase, uppercase | |
| `text-overflow` | clip, ellipsis | |
| `white-space` | normal, nowrap, pre | Use pre to format tabs and newlines |
| `word-break` | normal, break-all | |
| `content` | string | Sets the text of a Label: `content: "Loading…";` |

## Text Decoration

| Property | Values | Examples/Notes |
|----------|--------|----------------|
| `text-decoration` | Color, Length, LineStyle, Line | Properties can be in any order and multiple lines |
| `text-decoration-color` | Color | |
| `text-decoration-line` | underline, line-through, overline | Multiple properties: `text-decoration-line: overline underline;` |
| `text-decoration-thickness` | Length | |
| `text-decoration-underline-offset` | Length | |
| `text-decoration-overline-offset` | Length | |
| `text-decoration-line-through-offset` | Length | |
| `text-decoration-skip-ink` | all, none | Whether line decoration draws above glyphs |
| `text-shadow` | Same as box-shadow | |
| `text-background-angle` | Length | |

## Background & Images

| Property | Values | Examples/Notes |
|----------|--------|----------------|
| `background` | | Fills in the properties below |
| `background-color` | Color | |
| `background-image` | url(string), linear-gradient(Color, Color), radial-gradient(Color, Color), conic-gradient(Color, Color) | |
| `background-position` | Length, Length (optional) | `background-position: 10px`, `background-position: 10px 15px` |
| `background-position-x` | Length | |
| `background-position-y` | Length | |
| `background-repeat` | no-repeat, repeat-x, repeat-y, repeat | |
| `background-size` | Length, Length (optional) | `background-size: 10px`, `background-size: 10px 15px` |
| `background-size-x` | Length | |
| `background-size-y` | Length | |
| `background-angle` | Length | |
| `background-blend-mode` | normal, lighten, multiply | |
| `image-rendering` | auto (default), anisotropic, bilinear, trilinear, point, pixelated, nearest-neighbour | |

## Borders

| Property | Values | Examples/Notes |
|----------|--------|----------------|
| `border` | border-width, border-style, border-color | `border: 10px solid black;` |
| `border-top` | Same as border | |
| `border-right` | Same as border | |
| `border-bottom` | Same as border | |
| `border-left` | Same as border | |
| `border-color` | Color | |
| `border-width` | Length | |
| `border-top-color` | Color | |
| `border-right-color` | Color | |
| `border-bottom-color` | Color | |
| `border-left-color` | Color | |
| `border-top-width` | Length | |
| `border-right-width` | Length | |
| `border-bottom-width` | Length | |
| `border-left-width` | Length | |
| `border-radius` | Length | `border-radius: 8px;`, `border-radius: 8px 0px 8px 8px;` |
| `border-top-left-radius` | Length | |
| `border-top-right-radius` | Length | |
| `border-bottom-left-radius` | Length | |
| `border-bottom-right-radius` | Length | |
| `border-image` | Same as background-image | |
| `border-image-tint` | Color | |
| `border-image-width-top` | Length | |
| `border-image-width-right` | Length | |
| `border-image-width-bottom` | Length | |
| `border-image-width-left` | Length | |

## Effects & Filters

| Property | Values | Examples/Notes |
|----------|--------|----------------|
| `opacity` | Float | |
| `z-index` | int | |
| `overflow` | visible (default), hidden, scroll | |
| `overflow-x` | Same as overflow | |
| `overflow-y` | Same as overflow | |
| `box-shadow` | Length, Length (optional), Length (blur, optional), Length (spread, optional), Color | `box-shadow: 2px 2px 4px black;` |
| `filter` | Same as backdrop-filter | |
| `filter-blur` | Length | |
| `filter-brightness` | Length | |
| `filter-contrast` | Length | |
| `filter-hue-rotate` | Length | |
| `filter-invert` | Length | |
| `filter-saturate` | Length | |
| `filter-sepia` | Length | |
| `filter-tint` | Length | |
| `filter-drop-shadow` | Same as box-shadow | |
| `filter-border-color` | Color | |
| `filter-border-width` | Length | |
| `backdrop-filter` | blur(Length), saturate(Length), contrast(Length), brightness(Length), grayscale(Length), sepia(Length), hue-rotate(Length), invert(Length), border-wrap(Length, Color) | `backdrop-filter: blur(10px) saturate(80%);` |
| `backdrop-filter-blur` | Length | |
| `backdrop-filter-brightness` | Length | |
| `backdrop-filter-contrast` | Length | |
| `backdrop-filter-hue-rotate` | Length | |
| `backdrop-filter-invert` | Length | |
| `backdrop-filter-saturate` | Length | |
| `backdrop-filter-sepia` | Length | |
| `mix-blend-mode` | normal, lighten, multiply | |

## Masking

| Property | Values | Examples/Notes |
|----------|--------|----------------|
| `mask` | | Shorthand for other mask properties |
| `mask-image` | Same as background-image | |
| `mask-mode` | luminance, alpha | |
| `mask-position` | Length, Length (optional) | |
| `mask-position-x` | Length | |
| `mask-position-y` | Length | |
| `mask-repeat` | same as background-repeat | |
| `mask-size` | Length, Length (optional) | |
| `mask-size-x` | Length | |
| `mask-size-y` | Length | |
| `mask-angle` | Length | |

## Animations & Transitions

| Property | Values | Examples/Notes |
|----------|--------|----------------|
| `animation` | | Fills in the properties below |
| `animation-name` | String | |
| `animation-duration` | Float | |
| `animation-delay` | Float | |
| `animation-direction` | normal (default), reverse, alternate, alternate-reverse | |
| `animation-fill-mode` | none, forwards, backwards, both | |
| `animation-iteration-count` | int, infinite | |
| `animation-play-state` | running, paused | |
| `animation-timing-function` | linear (default), ease, ease-in, ease-out, ease-in-out | |
| `transition` | | `transition: all 0.1s ease;`, `transition: opacity 0.1s transform 0.2s ease-out;` |
| `transition-property` | String | |
| `transition-duration` | Float | |
| `transition-delay` | Float | |
| `transition-timing-function` | linear (default), ease, ease-in-out, ease-out, ease-in | |

## Transform & Perspective

| Property | Values | Examples/Notes |
|----------|--------|----------------|
| `transform` | | Fills in the properties below |
| `transform-origin` | Length, Length, Length (optional) | |
| `transform-origin-x` | Length | |
| `transform-origin-y` | Length | |
| `perspective-origin` | Length, Length (optional) | |
| `perspective-origin-x` | Length | |
| `perspective-origin-y` | Length | |

## Interaction

| Property | Values | Examples/Notes |
|----------|--------|----------------|
| `cursor` | none, pointer, progress, wait, crosshair, text, move, not-allowed, any custom cursors | |
| `pointer-events` | none (default), all, auto | |

## Custom Pseudo-Classes

S&Box-specific pseudo-classes for element lifecycle animations:

### `:intro`
- Removed when the element is created
- Things will transition away from this state
- Useful for creation animations

### `:outro` 
- Added when `Panel.Delete()` is called
- Panel waits for all transitions to finish before actually deleting
- Useful for deletion animations

### Example Usage
```scss
MyPanel {
    transition: all 2s ease-out;
    transform: scale(1);

    // When the element is created make it expand from nothing
    &:intro {
        transform: scale(0);
    }

    // When the element is deleted make it double in size before being deleted
    &:outro {
        transform: scale(2);
    }
}
```

## Important Notes

- Properties marked with * may behave differently than web standards
- `display` defaults to `flex` for everything
- `font-family` should specify font name, not filename
- `position` behavior follows Yoga layout rules
- All standard web pseudo-classes (`:hover`, `:active`, etc.) are supported
- Colors support alpha channels
- Lengths can be px, %, em, vw, vh, etc.