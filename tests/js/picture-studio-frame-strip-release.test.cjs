// 4.4.9 source regression: the GIF track is visually mounted next to the canvas,
// while the canvas and pre-existing GIF export route remain intact.
const fs = require('node:fs');
const path = require('node:path');
const assert = require('node:assert/strict');
const source = fs.readFileSync(path.join(__dirname, '../../src/PublisherStudio.Web/Components/Editor/PictureEditor.razor'), 'utf8');
const css = fs.readFileSync(path.join(__dirname, '../../src/PublisherStudio.Web/wwwroot/css/site.css'), 'utf8');
assert.match(source, /class="picture-gif-filmstrip"/);
assert.match(source, /State\.SetActiveAnimationFrame\(index\)/);
assert.match(source, /class="picture-canvas-scroll"/);
assert.match(source, /Apply animated GIF/);
assert.match(css, /\.picture-studio-canvas-host\.has-animation \.picture-canvas-scroll/);
console.log('Picture Studio GIF frame strip and original canvas route: passed.');
