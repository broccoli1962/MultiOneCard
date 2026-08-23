-- OneTable 89 fronts + BACK at 768x1080.
-- Cursor-generated art (Source/Gen) composited in Aseprite.

local OUT = [[D:/Unity/MultiOneCard/Assets/GameResource/Data/Cards]]
local GEN = OUT .. "/Source/Gen"
local SRC = OUT .. "/Source/OneTableCards.aseprite"
local LOG = OUT .. "/Source/compose_log.txt"
local W, H = 768, 1080
local CX, CY = 384, 540
local RAD = 36
local CLIP = 240

local function C(r, g, b, a)
  return Color{ r = r, g = g, b = b, a = a or 255 }
end

local FACE = C(248, 240, 220)
local FACE_INNER = C(238, 226, 200)
local INK_BLACK = C(28, 28, 32)
local INK_RED = C(196, 42, 48)
local INK_BLUE = C(36, 78, 186)
local GOLD = C(196, 164, 72)
local GOLD_DIM = C(164, 132, 56)
local GOLD_LIGHT = C(228, 204, 120)
local SPEC_BORDER = C(92, 72, 36)
local PILL_BK = C(36, 36, 40)
local PILL_RD = C(200, 48, 54)
local PILL_BL = C(40, 86, 196)

local GLYPH = {
  ["0"] = { ".###.", "#...#", "#...#", "#..##", "#.#.#", "##..#", "#...#", "#...#", ".###." },
  ["1"] = { "..#..", ".##..", "#.#..", "..#..", "..#..", "..#..", "..#..", "..#..", ".###." },
  ["2"] = { ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#....", "#...#", "#####" },
  ["3"] = { ".###.", "#...#", "....#", "..##.", "....#", "....#", "....#", "#...#", ".###." },
  ["4"] = { "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#.", "...#.", "..###" },
  ["5"] = { "#####", "#....", "#....", "####.", "....#", "....#", "....#", "#...#", ".###." },
  ["6"] = { ".###.", "#...#", "#....", "####.", "#...#", "#...#", "#...#", "#...#", ".###." },
  ["7"] = { "#####", "#...#", "....#", "...#.", "..#..", "..#..", ".#...", ".#...", ".#..." },
  ["8"] = { ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", "#...#", "#...#", ".###." },
  ["9"] = { ".###.", "#...#", "#...#", "#...#", ".####", "....#", "....#", "#...#", ".###." },
  ["A"] = { "..#..", ".#.#.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#", "#...#" },
  ["J"] = { ".####", "...#.", "...#.", "...#.", "...#.", "...#.", "#..#.", "#..#.", ".##.." },
  ["Q"] = { ".###.", "#...#", "#...#", "#...#", "#...#", "#.#.#", "#..##", "#...#", ".###." },
  ["K"] = { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#", "#...#", "#...#" },
  ["B"] = { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "#...#", "#...#", "####." },
  ["C"] = { ".###.", "#...#", "#....", "#....", "#....", "#....", "#....", "#...#", ".###." },
  ["E"] = { "#####", "#....", "#....", "####.", "#....", "#....", "#....", "#....", "#####" },
  ["I"] = { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#..", "..#..", "#####" },
  ["L"] = { "#....", "#....", "#....", "#....", "#....", "#....", "#....", "#....", "#####" },
  ["M"] = { "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#", "#...#", "#...#" },
  ["O"] = { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
  ["P"] = { "####.", "#...#", "#...#", "####.", "#....", "#....", "#....", "#....", "#...." },
  ["R"] = { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#", "#...#", "#...#" },
  ["S"] = { ".####", "#....", "#....", ".###.", "....#", "....#", "....#", "....#", "####." },
  ["T"] = { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
}

local function log(msg)
  local f = io.open(LOG, "a")
  if f then
    f:write(msg .. "\n")
    f:close()
  end
  print(msg)
end

local function clamp8(n)
  return math.max(0, math.min(255, math.floor(n + 0.5)))
end

local function lerpCol(a, b, t)
  return C(
    clamp8(a.red + (b.red - a.red) * t),
    clamp8(a.green + (b.green - a.green) * t),
    clamp8(a.blue + (b.blue - a.blue) * t)
  )
end

local function put(img, x, y, col)
  if x >= 0 and y >= 0 and x < W and y < H then
    img:putPixel(x, y, col)
  end
end

local function putBlend(img, x, y, col, a)
  if a <= 0.004 or x < 0 or y < 0 or x >= W or y >= H then
    return
  end
  if a >= 0.996 then
    img:putPixel(x, y, col)
    return
  end
  local bg = Color(img:getPixel(x, y))
  img:putPixel(x, y, lerpCol(bg, col, a))
end

local function stampCover(img, x, y, col, a)
  if a >= 1 then
    put(img, x, y, col)
  elseif a > 0 then
    putBlend(img, x, y, col, a)
  end
end

local function sdfRoundRect(px, py, w, h, rad)
  local qx = math.abs(px - (w - 1) * 0.5) - (w * 0.5 - rad)
  local qy = math.abs(py - (h - 1) * 0.5) - (h * 0.5 - rad)
  local ox, oy = math.max(qx, 0), math.max(qy, 0)
  return math.sqrt(ox * ox + oy * oy) + math.min(math.max(qx, qy), 0) - rad
end

local function fillRoundRect(img, col)
  for y = 0, H - 1 do
    for x = 0, W - 1 do
      stampCover(img, x, y, col, 0.5 - sdfRoundRect(x + 0.5, y + 0.5, W, H, RAD))
    end
  end
end

local RAIN_STOPS = {
  C(196, 42, 48),
  C(28, 28, 32),
  C(36, 78, 186),
  C(140, 40, 170),
}

local function atan2(y, x)
  if x > 0 then
    return math.atan(y / x)
  end
  if x < 0 and y >= 0 then
    return math.atan(y / x) + math.pi
  end
  if x < 0 then
    return math.atan(y / x) - math.pi
  end
  if y > 0 then
    return math.pi * 0.5
  end
  if y < 0 then
    return -math.pi * 0.5
  end
  return 0
end

local function rainbowAt(t)
  t = t % 1
  if t < 0 then
    t = t + 1
  end
  local n = #RAIN_STOPS
  local x = t * n
  local i = math.floor(x) % n
  local f = x - math.floor(x)
  return lerpCol(RAIN_STOPS[i + 1], RAIN_STOPS[(i + 1) % n + 1], f)
end

local function strokeRoundRect(img, inset, thick, col)
  local rw, rh = W - inset * 2, H - inset * 2
  local rad = math.max(6, RAD - inset)
  for y = 0, rh + 1 do
    for x = 0, rw + 1 do
      local d = sdfRoundRect(x + 0.5, y + 0.5, rw, rh, rad)
      stampCover(img, x + inset, y + inset, col, math.min(0.5 - d, d + thick + 0.5))
    end
  end
end

local function strokeRoundRectRainbow(img, inset, thick, phase)
  local rw, rh = W - inset * 2, H - inset * 2
  local rad = math.max(6, RAD - inset)
  for y = 0, rh + 1 do
    for x = 0, rw + 1 do
      local d = sdfRoundRect(x + 0.5, y + 0.5, rw, rh, rad)
      local a = math.min(0.5 - d, d + thick + 0.5)
      if a > 0 then
        local px, py = x + inset, y + inset
        local t = atan2(py - CY, px - CX) / (2 * math.pi) + 0.5 + (phase or 0)
        stampCover(img, px, py, rainbowAt(t), a)
      end
    end
  end
end

local function fillCircle(img, cx, cy, r, col)
  for y = math.floor(cy - r - 1), math.ceil(cy + r + 1) do
    for x = math.floor(cx - r - 1), math.ceil(cx + r + 1) do
      local dx, dy = x + 0.5 - cx, y + 0.5 - cy
      stampCover(img, x, y, col, r + 0.5 - math.sqrt(dx * dx + dy * dy))
    end
  end
end

local function strokeCircle(img, cx, cy, r, thick, col)
  local inner = r - thick
  for y = math.floor(cy - r - 1), math.ceil(cy + r + 1) do
    for x = math.floor(cx - r - 1), math.ceil(cx + r + 1) do
      local dx, dy = x + 0.5 - cx, y + 0.5 - cy
      local d = math.sqrt(dx * dx + dy * dy)
      stampCover(img, x, y, col, math.min(r + 0.5 - d, d - inner + 0.5))
    end
  end
end

local function stamp(img, rows, ox, oy, col, scale, flip)
  local hh, ww = #rows, #rows[1]
  for y = 1, hh do
    local row = rows[y]
    for x = 1, #row do
      if row:sub(x, x) == "#" then
        local px, py = x - 1, y - 1
        if flip then
          px = ww - x
          py = hh - y
        end
        for sy = 0, scale - 1 do
          for sx = 0, scale - 1 do
            put(img, ox + px * scale + sx, oy + py * scale + sy, col)
          end
        end
      end
    end
  end
end

local function textWidth(text, scale)
  local w = 0
  for i = 1, #text do
    local ch = text:sub(i, i)
    if GLYPH[ch] then
      w = w + (5 * scale) + scale
    elseif ch == " " then
      w = w + 3 * scale
    end
  end
  if w > 0 then
    w = w - scale
  end
  return w
end

local function stampText(img, text, ox, oy, col, scale, flip)
  local x = ox
  for i = 1, #text do
    local ch = text:sub(i, i)
    local g = GLYPH[ch]
    if g then
      stamp(img, g, x, oy, col, scale, flip)
      x = x + (5 * scale) + scale
    elseif ch == " " then
      x = x + 3 * scale
    end
  end
end

local function stampTextRainbow(img, text, ox, oy, scale, flip)
  local glyphs = 0
  for i = 1, #text do
    if GLYPH[text:sub(i, i)] then
      glyphs = glyphs + 1
    end
  end
  local k, x = 0, ox
  for i = 1, #text do
    local ch = text:sub(i, i)
    local g = GLYPH[ch]
    if g then
      stamp(img, g, x, oy, rainbowAt((k + 0.5) / math.max(glyphs, 1)), scale, flip)
      x = x + (5 * scale) + scale
      k = k + 1
    elseif ch == " " then
      x = x + 3 * scale
    end
  end
end

local function suitInk(suit)
  if suit == "H" or suit == "D" then
    return INK_RED
  end
  if suit == "R" or suit == "M" then
    return INK_BLUE
  end
  return INK_BLACK
end

local function loadImg(path)
  local img = Image{ fromFile = path }
  if img == nil then
    error("missing " .. path)
  end
  return img
end

local function isCream(c)
  local r, g, b = c.red, c.green, c.blue
  if r < 228 or g < 218 or b < 175 or b > 232 then
    return false
  end
  -- Warm parchment only. Neutral white (pill shells, ticket paper highlights) stays.
  return (r - b) >= 18 and (g - b) >= 12
end

local function punchCream(src)
  local punched = Image(src.width, src.height, ColorMode.RGB)
  punched:clear(Color{ r = 0, g = 0, b = 0, a = 0 })
  for y = 0, src.height - 1 do
    for x = 0, src.width - 1 do
      local c = Color(src:getPixel(x, y))
      if c.alpha > 20 and not isCream(c) then
        punched:putPixel(x, y, Color{ r = c.red, g = c.green, b = c.blue, a = 255 })
      end
    end
  end
  return punched
end

local rawCache = {}
local keyCache = {}
local sizedCache = {}

local function raw(name)
  if not rawCache[name] then
    rawCache[name] = loadImg(GEN .. "/" .. name)
  end
  return rawCache[name]
end

local function keyed(name)
  if not keyCache[name] then
    keyCache[name] = punchCream(raw(name))
  end
  return keyCache[name]
end

local function sized(name, nw, nh, punch)
  local key = (punch and "k:" or "r:") .. name .. ":" .. nw .. "x" .. nh
  if sizedCache[key] then
    return sizedCache[key]
  end
  local src = punch and keyed(name) or raw(name)
  local img = src:clone()
  if img.width ~= nw or img.height ~= nh then
    img:resize{ size = Size(nw, nh), method = "bilinear" }
  end
  sizedCache[key] = img
  return img
end

local function blit(dst, src, ox, oy)
  dst:drawImage(src, Point(ox, oy))
end

local function opaqueBounds(src)
  local minx, miny, maxx, maxy = src.width, src.height, -1, -1
  for y = 0, src.height - 1 do
    for x = 0, src.width - 1 do
      if Color(src:getPixel(x, y)).alpha > 20 then
        if x < minx then minx = x end
        if y < miny then miny = y end
        if x > maxx then maxx = x end
        if y > maxy then maxy = y end
      end
    end
  end
  if maxx < minx then
    return 0, 0, src.width - 1, src.height - 1
  end
  return minx, miny, maxx, maxy
end

local function fitKeyed(name, maxW, maxH, method)
  method = method or "bilinear"
  local key = "fit:" .. method .. ":" .. name .. ":" .. maxW .. "x" .. maxH
  if sizedCache[key] then
    return sizedCache[key]
  end
  local src = keyed(name)
  local x0, y0, x1, y1 = opaqueBounds(src)
  local cw, ch = x1 - x0 + 1, y1 - y0 + 1
  local crop = Image(cw, ch, ColorMode.RGB)
  crop:clear(Color{ r = 0, g = 0, b = 0, a = 0 })
  crop:drawImage(src, Point(-x0, -y0))
  local scale = math.min(maxW / cw, maxH / ch)
  local nw = math.max(1, math.floor(cw * scale + 0.5))
  local nh = math.max(1, math.floor(ch * scale + 0.5))
  if method == "nearest" then
    nw = math.max(4, math.floor(nw / 4) * 4)
    nh = math.max(4, math.floor(nh / 4) * 4)
  end
  if nw ~= crop.width or nh ~= crop.height then
    crop:resize{ size = Size(nw, nh), method = method }
  end
  sizedCache[key] = crop
  return crop
end

local function blitCentered(dst, src, cx, cy)
  blit(dst, src, math.floor(cx - src.width * 0.5), math.floor(cy - src.height * 0.5))
end

local function isNearCream(c)
  local r, g, b = c.red, c.green, c.blue
  if r < 215 or g < 200 or b > 230 then
    return false
  end
  return (r - b) >= 16 and (g - b) >= 10 and math.abs(r - g) < 22
end

local function fitRecolor(name, maxW, maxH)
  local key = "recolor:" .. name .. ":" .. maxW .. "x" .. maxH
  if sizedCache[key] then
    return sizedCache[key]
  end
  local src = raw(name):clone()
  for y = 0, src.height - 1 do
    for x = 0, src.width - 1 do
      local c = Color(src:getPixel(x, y))
      if isCream(c) or isNearCream(c) then
        src:putPixel(x, y, FACE_INNER)
      end
    end
  end
  local scale = math.min(maxW / src.width, maxH / src.height)
  local nw = math.max(1, math.floor(src.width * scale + 0.5))
  local nh = math.max(1, math.floor(src.height * scale + 0.5))
  if nw ~= src.width or nh ~= src.height then
    src:resize{ size = Size(nw, nh), method = "bilinear" }
  end
  local blended = src:clone()
  for y = 1, src.height - 2 do
    for x = 1, src.width - 2 do
      local c = Color(src:getPixel(x, y))
      if math.abs(c.red - FACE_INNER.red) > 4 or math.abs(c.green - FACE_INNER.green) > 4 then
        local faceN = 0
        for oy = -1, 1 do
          for ox = -1, 1 do
            local n = Color(src:getPixel(x + ox, y + oy))
            if math.abs(n.red - FACE_INNER.red) <= 4 and math.abs(n.green - FACE_INNER.green) <= 4 then
              faceN = faceN + 1
            end
          end
        end
        if faceN > 0 and faceN < 8 then
          blended:putPixel(x, y, lerpCol(c, FACE_INNER, 0.22 + 0.08 * faceN))
        end
      end
    end
  end
  sizedCache[key] = blended
  return blended
end

local function makeMoonDisc(name, r)
  local key = "moon:" .. name .. ":" .. r
  if sizedCache[key] then
    return sizedCache[key]
  end
  local d = r * 2
  local img = raw(name):clone()
  img:resize{ size = Size(d, d), method = "bilinear" }
  local disc = Image(d, d, ColorMode.RGB)
  disc:clear(Color{ r = 0, g = 0, b = 0, a = 0 })
  local rr = r - 0.5
  for y = 0, d - 1 do
    for x = 0, d - 1 do
      local dx, dy = x + 0.5 - r, y + 0.5 - r
      local dist = math.sqrt(dx * dx + dy * dy)
      local a = rr + 0.5 - dist
      if a > 0 then
        local c = Color(img:getPixel(x, y))
        local aa = 255
        if a < 1 then
          aa = clamp8(a * 255)
        end
        disc:putPixel(x, y, Color{ r = c.red, g = c.green, b = c.blue, a = aa })
      end
    end
  end
  sizedCache[key] = disc
  return disc
end

local function drawFiligree(img, border)
  local function arm(x, y, sx, sy, col)
    for i = 0, 40 do
      put(img, x + i * sx, y, GOLD)
      put(img, x, y + i * sy, GOLD)
    end
    fillCircle(img, x + 28 * sx, y, 3, GOLD_LIGHT)
    fillCircle(img, x, y + 28 * sy, 3, GOLD_LIGHT)
    fillCircle(img, x + 10 * sx, y + 10 * sy, 3, col)
  end
  arm(48, 48, 1, 1, border)
  arm(W - 49, 48, -1, 1, border)
  arm(48, H - 49, 1, -1, border)
  arm(W - 49, H - 49, -1, -1, border)
end

local function drawFiligreeRainbow(img)
  local function arm(x, y, sx, sy, col)
    for i = 0, 40 do
      put(img, x + i * sx, y, col)
      put(img, x, y + i * sy, col)
    end
    fillCircle(img, x + 28 * sx, y, 3, GOLD_LIGHT)
    fillCircle(img, x, y + 28 * sy, 3, GOLD_LIGHT)
    fillCircle(img, x + 10 * sx, y + 10 * sy, 3, col)
  end
  arm(48, 48, 1, 1, INK_RED)
  arm(W - 49, 48, -1, 1, INK_BLUE)
  arm(48, H - 49, 1, -1, INK_BLACK)
  arm(W - 49, H - 49, -1, -1, GOLD)
end

local function paintFaceBase(img, border)
  fillRoundRect(img, FACE)
  for y = 72, H - 73 do
    for x = 56, W - 57 do
      put(img, x, y, FACE_INNER)
    end
  end
  strokeRoundRect(img, 8, 7, border)
  strokeRoundRect(img, 22, 4, GOLD)
  drawFiligree(img, border)
end

local function paintFaceBaseRainbow(img)
  fillRoundRect(img, FACE)
  for y = 72, H - 73 do
    for x = 56, W - 57 do
      put(img, x, y, FACE_INNER)
    end
  end
  strokeRoundRectRainbow(img, 8, 7, 0)
  strokeRoundRectRainbow(img, 22, 4, 0.18)
  drawFiligreeRainbow(img)
end

local faceCache = {}
local function newFace(border)
  local key = border.red .. "," .. border.green .. "," .. border.blue
  if not faceCache[key] then
    local img = Image(W, H, ColorMode.RGB)
    img:clear(C(0, 0, 0))
    paintFaceBase(img, border)
    faceCache[key] = img
  end
  return faceCache[key]:clone()
end

local function newWildFace()
  if not faceCache.wild then
    local img = Image(W, H, ColorMode.RGB)
    img:clear(C(0, 0, 0))
    paintFaceBaseRainbow(img)
    faceCache.wild = img
  end
  return faceCache.wild:clone()
end

local function drawCaption(img, text, col)
  local scale = 6
  local tw = textWidth(text, scale)
  stampText(img, text, math.floor((W - tw) / 2), H - 92, col, scale, false)
end

local function drawCaptionRainbow(img, text)
  local scale = 6
  local tw = textWidth(text, scale)
  stampTextRainbow(img, text, math.floor((W - tw) / 2), H - 92, scale, false)
end

local function drawCornerIndex(img, rank, suit, ink)
  local scale = 8
  stampText(img, rank, 40, 36, ink, scale, false)
  blit(img, sized("suit_" .. suit .. ".png", 72, 72, true), 40, 120)
  local tw = textWidth(rank, scale)
  stampText(img, rank, W - 40 - tw, H - 36 - 9 * scale, ink, scale, true)
  blit(img, sized("suit_" .. suit .. ".png", 72, 72, true), W - 112, H - 192)
end

local function pipBox()
  return 156, 192, W - 156, H - 192
end

local function mapPip(fx, fy)
  local x0, y0, x1, y1 = pipBox()
  return math.floor(x0 + (x1 - x0) * fx), math.floor(y0 + (y1 - y0) * fy)
end

local function pipLayout(rank)
  local n = tonumber(rank)
  local p = {}
  local function add(fx, fy)
    p[#p + 1] = { fx, fy }
  end
  if n == 2 then
    add(0.50, 0.18); add(0.50, 0.82)
  elseif n == 3 then
    add(0.50, 0.18); add(0.50, 0.50); add(0.50, 0.82)
  elseif n == 4 then
    add(0.30, 0.20); add(0.70, 0.20); add(0.30, 0.80); add(0.70, 0.80)
  elseif n == 5 then
    add(0.30, 0.20); add(0.70, 0.20); add(0.50, 0.50); add(0.30, 0.80); add(0.70, 0.80)
  elseif n == 6 then
    add(0.30, 0.18); add(0.70, 0.18); add(0.30, 0.50); add(0.70, 0.50); add(0.30, 0.82); add(0.70, 0.82)
  elseif n == 7 then
    add(0.30, 0.16); add(0.70, 0.16); add(0.50, 0.34); add(0.30, 0.50); add(0.70, 0.50); add(0.30, 0.84); add(0.70, 0.84)
  elseif n == 8 then
    add(0.30, 0.16); add(0.70, 0.16); add(0.50, 0.32); add(0.30, 0.50); add(0.70, 0.50); add(0.50, 0.68); add(0.30, 0.84); add(0.70, 0.84)
  elseif n == 9 then
    add(0.30, 0.14); add(0.70, 0.14); add(0.30, 0.34); add(0.70, 0.34); add(0.50, 0.50); add(0.30, 0.66); add(0.70, 0.66); add(0.30, 0.86); add(0.70, 0.86)
  elseif n == 10 then
    add(0.30, 0.12); add(0.70, 0.12); add(0.30, 0.32); add(0.70, 0.32); add(0.50, 0.24); add(0.50, 0.76); add(0.30, 0.68); add(0.70, 0.68); add(0.30, 0.88); add(0.70, 0.88)
  end
  return p
end

local function drawTrump(suit, rank)
  local ink = suitInk(suit)
  local img = newFace(ink)
  drawCornerIndex(img, rank, suit, ink)
  local suitFile = "suit_" .. suit .. ".png"
  if rank == "A" then
    blit(img, sized(suitFile, 300, 300, true), CX - 150, CY - 170)
  elseif rank == "J" or rank == "Q" or rank == "K" then
    blit(img, sized("court_" .. rank .. ".png", 488, 650, true), 140, 150)
    blit(img, sized(suitFile, 72, 72, true), CX - 36, 820)
  else
    local layout = pipLayout(rank)
    local pip = sized(suitFile, 88, 88, true)
    for i = 1, #layout do
      local px, py = mapPip(layout[i][1], layout[i][2])
      blit(img, pip, px - 44, py - 44)
    end
  end
  return img
end

local function drawJoker(kind)
  local moonFile, border, fill, ring
  if kind == "BW" then
    moonFile, border, fill, ring = "moon_bw.png", INK_BLACK, C(16, 16, 20), C(200, 200, 208)
  elseif kind == "COLOR" then
    moonFile, border, fill, ring = "moon_color.png", C(200, 40, 48), C(42, 10, 14), C(220, 64, 72)
  else
    moonFile, border, fill, ring = "moon_blue.png", C(40, 86, 196), C(8, 14, 38), C(120, 170, 255)
  end
  local img = newWildFace()
  local r = CLIP - 6
  fillCircle(img, CX, CY, CLIP, fill)
  blit(img, makeMoonDisc(moonFile, r), CX - r, CY - r)
  strokeCircle(img, CX, CY, CLIP, 7, ring)
  strokeCircle(img, CX, CY, CLIP - 10, 3, GOLD_DIM)
  return img
end

local function drawSpecial(id)
  local file, caption, maxW, maxH, cy, method
  if id == "SPEC:SPEAR" then
    file, caption, maxW, maxH, cy = "spec_spear.png", "SPEAR", 420, 700, 500
  elseif id == "SPEC:PASS" then
    file, caption, maxW, maxH, cy = "spec_pass.png", "PASS", 560, 420, 500
  elseif id == "SPEC:REVJOKER" then
    file, caption, maxW, maxH, cy = "spec_rev.png", "RE", 520, 520, 500
  elseif id == "SPEC:COUNTER" then
    file, caption, maxW, maxH, cy, method = "spec_counter.png", "CTR", 540, 640, 500, "nearest"
  elseif id == "SPEC:MIRROR" then
    file, caption, maxW, maxH, cy = "spec_mirror.png", "MIRROR", 480, 640, 500
  elseif id == "SPEC:PILL_BK" then
    file, caption, maxW, maxH, cy = "spec_pill_bk.png", "PILL", 520, 240, 500
  elseif id == "SPEC:PILL_RD" then
    file, caption, maxW, maxH, cy = "spec_pill_rd.png", "PILL", 520, 240, 500
  else
    file, caption, maxW, maxH, cy = "spec_pill_bl.png", "PILL", 520, 240, 500
  end
  local img = newWildFace()
  blitCentered(img, fitKeyed(file, maxW, maxH, method), CX, cy)

  drawCaptionRainbow(img, caption)
  return img
end

local function drawBack()
  local img = Image(W, H, ColorMode.RGB)
  img:clear(C(0, 0, 0))
  local src = sized("card_back.png", W, H, false)
  for y = 0, H - 1 do
    for x = 0, W - 1 do
      local a = 0.5 - sdfRoundRect(x + 0.5, y + 0.5, W, H, RAD)
      if a > 0 then
        stampCover(img, x, y, Color(src:getPixel(x, y)), a)
      end
    end
  end
  strokeRoundRect(img, 8, 7, GOLD)
  return img
end

local function fileName(defId)
  return defId:gsub(":", "_") .. ".png"
end

local suits = { "S", "H", "D", "C", "R", "M" }
local ranks = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" }
local specials = {
  "JOKER:BW", "JOKER:COLOR", "JOKER:MOON",
  "SPEC:SPEAR", "SPEC:PASS", "SPEC:REVJOKER", "SPEC:COUNTER", "SPEC:MIRROR",
  "SPEC:PILL_BK", "SPEC:PILL_RD", "SPEC:PILL_BL",
}

local cards = {}
for _, id in ipairs(specials) do
  cards[#cards + 1] = { id = id, kind = "spec" }
end

do
  local f = io.open(LOG, "w")
  if f then
    f:write("compose wilds start\n")
    f:close()
  end
end

local spr
local opened, openedSpr = pcall(function()
  return app.open(SRC)
end)
if opened and openedSpr then
  spr = openedSpr
else
  spr = Sprite(W, H, ColorMode.RGB)
  spr.filename = SRC
end
app.activeSprite = spr
local bgLayer = spr.layers[1]
bgLayer.name = "Background"
while #spr.frames < #cards do
  spr:newFrame()
end

local function paintCel(frame, img)
  local cel = bgLayer:cel(frame) or spr:newCel(bgLayer, frame)
  cel.image = img
  cel.position = Point(0, 0)
end

for i, card in ipairs(cards) do
  local ok, composed = pcall(function()
    if card.id == "JOKER:BW" then
      return drawJoker("BW")
    elseif card.id == "JOKER:COLOR" then
      return drawJoker("COLOR")
    elseif card.id == "JOKER:MOON" then
      return drawJoker("MOON")
    end
    return drawSpecial(card.id)
  end)
  if not ok then
    log("error " .. card.id .. " " .. tostring(composed))
    error(composed)
  end
  paintCel(spr.frames[i], composed)
  composed:saveAs(OUT .. "/" .. fileName(card.id))
  log(string.format("card %d/%d %s", i, #cards, card.id))
end

if opened and openedSpr then
  spr:saveAs(SRC)
end
log("exported " .. #cards .. " wild fronts at " .. W .. "x" .. H)
