-- OneTable Official 89 CardDefId fronts + BACK at native 384x540 (32:45).
-- Clean AA illustration: no paper grain, no dither hatch. Not a 64x90 upscale.

local OUT = [[D:/Unity/MultiOneCard/Assets/GameResource/Data/Cards]]
local SRC = OUT .. "/Source/OneTableCards.aseprite"
local W, H = 384, 540
local CX, CY = 192, 270
local RAD = 18

local function C(r, g, b, a)
  return Color{ r = r, g = g, b = b, a = a or 255 }
end

local FACE = C(248, 240, 220)
local FACE_INNER = C(236, 224, 196)
local INK_BLACK = C(28, 28, 32)
local INK_RED = C(196, 42, 48)
local INK_BLUE = C(36, 78, 186)
local GOLD = C(196, 164, 72)
local GOLD_DIM = C(164, 132, 56)
local GOLD_LIGHT = C(228, 204, 120)
local NAVY = C(24, 32, 58)
local NAVY_DARK = C(14, 18, 36)
local NAVY_LIGHT = C(42, 56, 96)
local NAVY_MID = C(30, 42, 78)
local SPEC_BORDER = C(92, 72, 36)
local WHITE = C(255, 255, 255)
local PILL_BK = C(36, 36, 40)
local PILL_RD = C(200, 48, 54)
local PILL_BL = C(40, 86, 196)
local JOKER_RED = C(200, 40, 48)
local JOKER_BLUE = C(40, 86, 196)
local BLOOD = C(176, 16, 28)
local BLOOD_DARK = C(120, 8, 16)
local BLOOD_LIGHT = C(210, 48, 56)
local MOON_FILL = C(232, 232, 240)
local MOON_SHADE = C(176, 176, 188)
local MOON_INK = C(36, 36, 42)
local CRESCENT_RED = C(232, 72, 80)
local CRESCENT_RED_SHADE = C(160, 28, 36)
local SKIN = C(234, 198, 166)
local SKIN_D = C(204, 156, 122)
local SKIN_L = C(248, 222, 194)
local LIP = C(168, 72, 80)
local WOOD = C(110, 78, 46)
local WOOD_L = C(156, 112, 68)
local WOOD_D = C(78, 52, 30)
local STEEL = C(72, 76, 84)
local STEEL_L = C(168, 176, 188)
local STEEL_D = C(40, 42, 48)

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
  ["D"] = { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", "####." },
  ["E"] = { "#####", "#....", "#....", "####.", "#....", "#....", "#....", "#....", "#####" },
  ["G"] = { ".###.", "#...#", "#....", "#....", "#.###", "#...#", "#...#", "#...#", ".####" },
  ["H"] = { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#", "#...#", "#...#" },
  ["I"] = { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#..", "..#..", "#####" },
  ["L"] = { "#....", "#....", "#....", "#....", "#....", "#....", "#....", "#....", "#####" },
  ["M"] = { "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#", "#...#", "#...#" },
  ["N"] = { "#...#", "##..#", "##..#", "#.#.#", "#.#.#", "#..##", "#..##", "#...#", "#...#" },
  ["O"] = { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
  ["P"] = { "####.", "#...#", "#...#", "####.", "#....", "#....", "#....", "#....", "#...." },
  ["R"] = { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#", "#...#", "#...#" },
  ["S"] = { ".####", "#....", "#....", ".###.", "....#", "....#", "....#", "....#", "####." },
  ["T"] = { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
  ["U"] = { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
  ["V"] = { "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", ".#.#.", "..#..", "..#.." },
  ["W"] = { "#...#", "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "##.##", "##.##", "#...#" },
  ["X"] = { "#...#", "#...#", ".#.#.", "..#..", "..#..", ".#.#.", "#...#", "#...#", "#...#" },
  ["Y"] = { "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
  ["+"] = { ".....", "..#..", "..#..", "#####", "..#..", "..#..", ".....", ".....", "....." },
}

local function clamp8(n)
  return math.max(0, math.min(255, math.floor(n + 0.5)))
end

local function shade(col, f)
  return C(clamp8(col.red * f), clamp8(col.green * f), clamp8(col.blue * f), col.alpha)
end

local function lerpCol(a, b, t)
  return C(
    clamp8(a.red + (b.red - a.red) * t),
    clamp8(a.green + (b.green - a.green) * t),
    clamp8(a.blue + (b.blue - a.blue) * t)
  )
end

local function sameRgb(px, col)
  local c = Color(px)
  return c.red == col.red and c.green == col.green and c.blue == col.blue
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
  img:putPixel(x, y, C(
    clamp8(bg.red + (col.red - bg.red) * a),
    clamp8(bg.green + (col.green - bg.green) * a),
    clamp8(bg.blue + (col.blue - bg.blue) * a)
  ))
end

local function cover(a)
  if a >= 1 then
    return 1
  end
  if a <= 0 then
    return 0
  end
  return a
end

local function stampCover(img, x, y, col, a)
  a = cover(a)
  if a >= 1 then
    put(img, x, y, col)
  elseif a > 0 then
    putBlend(img, x, y, col, a)
  end
end

local function fillRect(img, x, y, w, h, col)
  for iy = y, y + h - 1 do
    for ix = x, x + w - 1 do
      put(img, ix, iy, col)
    end
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

local function strokeRoundRect(img, inset, thick, col)
  local rw, rh = W - inset * 2, H - inset * 2
  local rad = math.max(4, RAD - inset)
  for y = 0, rh + 1 do
    for x = 0, rw + 1 do
      local d = sdfRoundRect(x + 0.5, y + 0.5, rw, rh, rad)
      local a = math.min(0.5 - d, d + thick + 0.5)
      stampCover(img, x + inset, y + inset, col, a)
    end
  end
end

local function fillCircle(img, cx, cy, r, col)
  local minx, maxx = math.floor(cx - r - 1), math.ceil(cx + r + 1)
  local miny, maxy = math.floor(cy - r - 1), math.ceil(cy + r + 1)
  for y = miny, maxy do
    for x = minx, maxx do
      local dx, dy = x + 0.5 - cx, y + 0.5 - cy
      stampCover(img, x, y, col, r + 0.5 - math.sqrt(dx * dx + dy * dy))
    end
  end
end

local function fillEllipse(img, cx, cy, rx, ry, col)
  local minx, maxx = math.floor(cx - rx - 1), math.ceil(cx + rx + 1)
  local miny, maxy = math.floor(cy - ry - 1), math.ceil(cy + ry + 1)
  for y = miny, maxy do
    for x = minx, maxx do
      local nx = (x + 0.5 - cx) / (rx + 0.0001)
      local ny = (y + 0.5 - cy) / (ry + 0.0001)
      local d = math.sqrt(nx * nx + ny * ny)
      stampCover(img, x, y, col, (1.0 - d) * math.min(rx, ry) + 0.5)
    end
  end
end

local function strokeCircle(img, cx, cy, r, thick, col)
  local minx, maxx = math.floor(cx - r - 1), math.ceil(cx + r + 1)
  local miny, maxy = math.floor(cy - r - 1), math.ceil(cy + r + 1)
  local inner = r - thick
  for y = miny, maxy do
    for x = minx, maxx do
      local dx, dy = x + 0.5 - cx, y + 0.5 - cy
      local d = math.sqrt(dx * dx + dy * dy)
      stampCover(img, x, y, col, math.min(r + 0.5 - d, d - inner + 0.5))
    end
  end
end

local function strokeEllipse(img, cx, cy, rx, ry, thick, col)
  local minx, maxx = math.floor(cx - rx - 1), math.ceil(cx + rx + 1)
  local miny, maxy = math.floor(cy - ry - 1), math.ceil(cy + ry + 1)
  local k = math.min(rx, ry)
  for y = miny, maxy do
    for x = minx, maxx do
      local nx = (x + 0.5 - cx) / (rx + 0.0001)
      local ny = (y + 0.5 - cy) / (ry + 0.0001)
      local d = (math.sqrt(nx * nx + ny * ny) - 1) * k
      stampCover(img, x, y, col, math.min(0.5 - d, d + thick + 0.5))
    end
  end
end

local function inPoly(px, py, verts)
  local n = #verts
  local inside = false
  local j = n
  for i = 1, n do
    local xi, yi = verts[i][1], verts[i][2]
    local xj, yj = verts[j][1], verts[j][2]
    if ((yi > py) ~= (yj > py)) and (px < (xj - xi) * (py - yi) / (yj - yi) + xi) then
      inside = not inside
    end
    j = i
  end
  return inside
end

local function fillPoly(img, verts, col)
  local minx, miny, maxx, maxy = W, H, 0, 0
  for i = 1, #verts do
    minx = math.min(minx, verts[i][1])
    miny = math.min(miny, verts[i][2])
    maxx = math.max(maxx, verts[i][1])
    maxy = math.max(maxy, verts[i][2])
  end
  for y = math.floor(miny), math.ceil(maxy) do
    for x = math.floor(minx), math.ceil(maxx) do
      if inPoly(x + 0.5, y + 0.5, verts) then
        put(img, x, y, col)
      end
    end
  end
end

local function starVerts(cx, cy, ro, ri, n)
  local v = {}
  for i = 0, n * 2 - 1 do
    local a = -math.pi / 2 + i * math.pi / n
    local r = (i % 2 == 0) and ro or ri
    v[#v + 1] = { cx + math.cos(a) * r, cy + math.sin(a) * r }
  end
  return v
end

local function fillStar(img, cx, cy, ro, ri, col)
  fillPoly(img, starVerts(cx, cy, ro, ri, 5), col)
end

local function fillSparkle(img, cx, cy, arm, col)
  fillRect(img, cx - arm, cy, arm * 2 + 1, 1, col)
  fillRect(img, cx, cy - arm, 1, arm * 2 + 1, col)
  if arm >= 3 then
    put(img, cx - 1, cy - 1, col)
    put(img, cx + 1, cy - 1, col)
    put(img, cx - 1, cy + 1, col)
    put(img, cx + 1, cy + 1, col)
  end
end

local function fillDiamond(img, cx, cy, hw, hh, col)
  for y = math.floor(cy - hh - 1), math.ceil(cy + hh + 1) do
    for x = math.floor(cx - hw - 1), math.ceil(cx + hw + 1) do
      local ty = 1 - math.abs(y + 0.5 - cy) / math.max(0.001, hh)
      local w = hw * math.max(0, ty)
      stampCover(img, x, y, col, w + 0.5 - math.abs(x + 0.5 - cx))
    end
  end
end

local function fillHeart(img, cx, cy, s, col, flip)
  local r = math.max(4, s * 0.26)
  local oy = flip and 1 or -1
  local top = cy + oy * (s * 0.12)
  local y0 = cy + oy * (s * 0.02)
  local y1 = cy - oy * (s * 0.50)
  local half = s * 0.52
  local miny = math.floor(math.min(y0, y1, top - r) - 1)
  local maxy = math.ceil(math.max(y0, y1, top + r) + 1)
  for y = miny, maxy do
    for x = math.floor(cx - half - r - 1), math.ceil(cx + half + r + 1) do
      local dx1, dy1 = x + 0.5 - (cx - r), y + 0.5 - top
      local dx2, dy2 = x + 0.5 - (cx + r), y + 0.5 - top
      local a = math.max(r + 0.5 - math.sqrt(dx1 * dx1 + dy1 * dy1), r + 0.5 - math.sqrt(dx2 * dx2 + dy2 * dy2))
      local denom = y1 - y0
      if denom ~= 0 then
        local t = (y + 0.5 - y0) / denom
        if t >= 0 and t <= 1 then
          a = math.max(a, half * (1 - t) + 0.5 - math.abs(x + 0.5 - cx))
        end
      end
      stampCover(img, x, y, col, a)
    end
  end
end

local function fillSpade(img, cx, cy, s, col, flip)
  fillHeart(img, cx, cy + (flip and 4 or -4), s, col, not flip)
  local stemH = math.floor(s * 0.28)
  local stemW = math.max(3, math.floor(s * 0.10))
  local stemY = flip and (cy - math.floor(s * 0.42) - stemH) or (cy + math.floor(s * 0.18))
  fillRect(img, cx - math.floor(stemW / 2), stemY, stemW, stemH, col)
  local baseY = flip and (cy - math.floor(s * 0.48)) or (cy + math.floor(s * 0.40))
  local baseH = math.floor(s * 0.12)
  local dir = flip and -1 or 1
  for i = 0, baseH do
    local w = math.floor(s * 0.22) - i
    fillRect(img, cx - w, baseY + i * dir, w * 2 + 1, 1, col)
  end
end

local function fillClub(img, cx, cy, s, col, flip)
  local r = math.max(5, math.floor(s * 0.22))
  local dy = flip and 6 or -6
  fillCircle(img, cx, cy + dy - (flip and -r or r), r, col)
  fillCircle(img, cx - r, cy + dy, r, col)
  fillCircle(img, cx + r, cy + dy, r, col)
  local stemH = math.floor(s * 0.30)
  local stemW = math.max(3, math.floor(s * 0.10))
  local stemY = flip and (cy - math.floor(s * 0.36) - stemH) or (cy + math.floor(s * 0.10))
  fillRect(img, cx - math.floor(stemW / 2), stemY, stemW, stemH, col)
  local baseY = flip and (cy - math.floor(s * 0.42)) or (cy + math.floor(s * 0.36))
  local dir = flip and -1 or 1
  for i = 0, math.floor(s * 0.10) do
    local w = math.floor(s * 0.18) - i
    fillRect(img, cx - w, baseY + i * dir, w * 2 + 1, 1, col)
  end
end

local function fillMoonSuit(img, cx, cy, s, col, flip)
  local r = s * 0.46
  local ox = cx + s * 0.16
  local oy = cy + (flip and 4 or -4)
  local r2 = s * 0.38
  for y = math.floor(cy - r - 1), math.ceil(cy + r + 1) do
    for x = math.floor(cx - r - 1), math.ceil(cx + r + 1) do
      local dx, dy = x + 0.5 - cx, y + 0.5 - cy
      local dx2, dy2 = x + 0.5 - ox, y + 0.5 - oy
      local a = math.min(r + 0.5 - math.sqrt(dx * dx + dy * dy), math.sqrt(dx2 * dx2 + dy2 * dy2) - r2 + 0.5)
      stampCover(img, x, y, col, a)
    end
  end
end

local function fillSuit(img, suit, cx, cy, size, col, flip)
  if suit == "H" then
    fillHeart(img, cx, cy, size, col, flip)
  elseif suit == "D" then
    fillDiamond(img, cx, cy, math.floor(size * 0.42), math.floor(size * 0.50), col)
  elseif suit == "S" then
    fillSpade(img, cx, cy, size, col, flip)
  elseif suit == "C" then
    fillClub(img, cx, cy, size, col, flip)
  elseif suit == "R" then
    fillStar(img, cx, cy, size * 0.50, size * 0.20, col)
  else
    fillMoonSuit(img, cx, cy, size, col, flip)
  end
end

local function shadeSuitVolume(img, cx, cy, reach, col)
  local hi, lo = shade(col, 1.18), shade(col, 0.78)
  for y = cy - reach, cy + reach do
    for x = cx - reach, cx + reach do
      if x >= 0 and y >= 0 and x < W and y < H and sameRgb(img:getPixel(x, y), col) then
        local t = ((x - cx) + (y - cy)) / math.max(1, reach * 1.4)
        t = math.max(0, math.min(1, (t + 1) * 0.5))
        put(img, x, y, lerpCol(hi, lo, t))
      end
    end
  end
end

local function drawSuitDetailed(img, suit, cx, cy, size, col, flip)
  fillSuit(img, suit, cx + 0.8, cy + 1.2, size + 1.5, shade(col, 0.55), flip)
  fillSuit(img, suit, cx, cy, size, col, flip)
  shadeSuitVolume(img, cx, cy, math.floor(size * 0.72), col)
end

local function stamp(img, rows, ox, oy, col, scale, flip)
  scale = scale or 1
  local hh = #rows
  local ww = #rows[1]
  for y = 1, hh do
    local row = rows[y]
    for x = 1, #row do
      if row:sub(x, x) == "#" then
        local px, py = x - 1, y - 1
        if flip then
          px = ww - x
          py = hh - y
        end
        if scale <= 1 then
          put(img, ox + px, oy + py, col)
        else
          for sy = 0, scale - 1 do
            for sx = 0, scale - 1 do
              put(img, ox + px * scale + sx, oy + py * scale + sy, col)
            end
          end
        end
      end
    end
  end
end

local function stampText(img, text, ox, oy, col, scale, flip)
  scale = scale or 1
  local x = ox
  local gh = #GLYPH["A"]
  local gw = 5
  for i = 1, #text do
    local ch = text:sub(i, i)
    local g = GLYPH[ch]
    if g then
      stamp(img, g, x, oy, col, scale, flip)
      x = x + (gw * scale) + scale
    elseif ch == " " then
      x = x + 3 * scale
    end
  end
  return x - ox, gh * scale
end

local function textWidth(text, scale)
  scale = scale or 1
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

local function suitInk(suit)
  if suit == "H" or suit == "D" then
    return INK_RED
  end
  if suit == "R" or suit == "M" then
    return INK_BLUE
  end
  return INK_BLACK
end

local function hairOf(suit)
  if suit == "H" or suit == "D" then
    return C(92, 40, 44)
  end
  if suit == "R" or suit == "M" then
    return C(24, 36, 82)
  end
  return C(22, 20, 26)
end

local function drawFiligree(img, border)
  local function arm(x, y, sx, sy)
    for i = 0, 22 do
      put(img, x + i * sx, y, GOLD)
      put(img, x, y + i * sy, GOLD)
    end
    fillCircle(img, x + 16 * sx, y, 2, GOLD_LIGHT)
    fillCircle(img, x, y + 16 * sy, 2, GOLD_LIGHT)
    fillCircle(img, x + 6 * sx, y + 6 * sy, 2, border)
  end
  arm(26, 26, 1, 1)
  arm(W - 27, 26, -1, 1)
  arm(26, H - 27, 1, -1)
  arm(W - 27, H - 27, -1, -1)
end

local function drawFaceBase(img, border)
  fillRoundRect(img, FACE)
  fillRect(img, 30, 38, W - 60, H - 76, FACE_INNER)
  strokeRoundRect(img, 4, 4, border)
  strokeRoundRect(img, 12, 2, GOLD)
  drawFiligree(img, border)
end

local function drawCornerIndex(img, rank, suit, ink)
  local scale = 4
  stampText(img, rank, 22, 20, ink, scale, false)
  drawSuitDetailed(img, suit, 36, 78, 28, ink, false)
  local tw = textWidth(rank, scale)
  local rh = 9 * scale
  stampText(img, rank, W - 22 - tw, H - 20 - rh, ink, scale, true)
  drawSuitDetailed(img, suit, W - 36, H - 78, 28, ink, true)
end

local function pipBox()
  return 78, 96, W - 78, H - 96
end

local function mapPip(fx, fy)
  local x0, y0, x1, y1 = pipBox()
  return math.floor(x0 + (x1 - x0) * fx), math.floor(y0 + (y1 - y0) * fy)
end

local function pipLayout(rank)
  if rank == "A" or rank == "J" or rank == "Q" or rank == "K" then
    return { { 0.50, 0.50, false, "large" } }
  end
  local n = tonumber(rank)
  local p = {}
  local function add(fx, fy, flip)
    p[#p + 1] = { fx, fy, flip, "pip" }
  end
  if n == 2 then
    add(0.50, 0.18, false)
    add(0.50, 0.82, true)
  elseif n == 3 then
    add(0.50, 0.18, false)
    add(0.50, 0.50, false)
    add(0.50, 0.82, true)
  elseif n == 4 then
    add(0.30, 0.20, false)
    add(0.70, 0.20, false)
    add(0.30, 0.80, true)
    add(0.70, 0.80, true)
  elseif n == 5 then
    add(0.30, 0.20, false)
    add(0.70, 0.20, false)
    add(0.50, 0.50, false)
    add(0.30, 0.80, true)
    add(0.70, 0.80, true)
  elseif n == 6 then
    add(0.30, 0.18, false)
    add(0.70, 0.18, false)
    add(0.30, 0.50, false)
    add(0.70, 0.50, false)
    add(0.30, 0.82, true)
    add(0.70, 0.82, true)
  elseif n == 7 then
    add(0.30, 0.16, false)
    add(0.70, 0.16, false)
    add(0.50, 0.34, false)
    add(0.30, 0.50, false)
    add(0.70, 0.50, false)
    add(0.30, 0.84, true)
    add(0.70, 0.84, true)
  elseif n == 8 then
    add(0.30, 0.16, false)
    add(0.70, 0.16, false)
    add(0.50, 0.32, false)
    add(0.30, 0.50, false)
    add(0.70, 0.50, false)
    add(0.50, 0.68, true)
    add(0.30, 0.84, true)
    add(0.70, 0.84, true)
  elseif n == 9 then
    add(0.30, 0.14, false)
    add(0.70, 0.14, false)
    add(0.30, 0.34, false)
    add(0.70, 0.34, false)
    add(0.50, 0.50, false)
    add(0.30, 0.66, true)
    add(0.70, 0.66, true)
    add(0.30, 0.86, true)
    add(0.70, 0.86, true)
  elseif n == 10 then
    add(0.30, 0.12, false)
    add(0.70, 0.12, false)
    add(0.30, 0.32, false)
    add(0.70, 0.32, false)
    add(0.50, 0.24, false)
    add(0.50, 0.76, true)
    add(0.30, 0.68, true)
    add(0.70, 0.68, true)
    add(0.30, 0.88, true)
    add(0.70, 0.88, true)
  end
  return p
end

local function inOval(x, y, cx, cy, rx, ry)
  local dx, dy = (x - cx) / rx, (y - cy) / ry
  return dx * dx + dy * dy <= 1
end

local function putOval(img, x, y, col, ocx, ocy, rx, ry)
  local nx = (x + 0.5 - ocx) / rx
  local ny = (y + 0.5 - ocy) / ry
  local d = (math.sqrt(nx * nx + ny * ny) - 1) * math.min(rx, ry)
  stampCover(img, x, y, col, 0.5 - d)
end

local function fillEllipseOval(img, cx, cy, rx, ry, col, ocx, ocy, orx, ory)
  for y = cy - ry, cy + ry do
    for x = cx - rx, cx + rx do
      local dx, dy = x - cx, y - cy
      if dx * dx * ry * ry + dy * dy * rx * rx <= rx * rx * ry * ry then
        putOval(img, x, y, col, ocx, ocy, orx, ory)
      end
    end
  end
end

local function fillRectOval(img, x, y, w, h, col, ocx, ocy, rx, ry)
  for iy = y, y + h - 1 do
    for ix = x, x + w - 1 do
      putOval(img, ix, iy, col, ocx, ocy, rx, ry)
    end
  end
end

local function drawEyes(img, cx, cy, ink, ocx, ocy, rx, ry)
  fillEllipseOval(img, cx - 13, cy, 8, 6, WHITE, ocx, ocy, rx, ry)
  fillEllipseOval(img, cx + 13, cy, 8, 6, WHITE, ocx, ocy, rx, ry)
  fillEllipseOval(img, cx - 11, cy + 1, 4, 4, ink, ocx, ocy, rx, ry)
  fillEllipseOval(img, cx + 15, cy + 1, 4, 4, ink, ocx, ocy, rx, ry)
  putOval(img, cx - 10, cy, WHITE, ocx, ocy, rx, ry)
  putOval(img, cx + 16, cy, WHITE, ocx, ocy, rx, ry)
  for i = -7, 7 do
    putOval(img, cx - 13 + i, cy - 6, SKIN_D, ocx, ocy, rx, ry)
    putOval(img, cx + 13 + i, cy - 6, SKIN_D, ocx, ocy, rx, ry)
  end
end

local function drawNoseMouth(img, cx, cy, ink, lips, ocx, ocy, rx, ry)
  fillEllipseOval(img, cx + 1, cy + 12, 3, 6, SKIN_D, ocx, ocy, rx, ry)
  putOval(img, cx + 2, cy + 16, shade(SKIN_D, 0.85), ocx, ocy, rx, ry)
  for i = 0, 16 do
    local t = i / 16
    local x = cx - 8 + i
    local y = cy + 26 + math.floor(math.sin(t * math.pi) * 3)
    putOval(img, x, y, lips, ocx, ocy, rx, ry)
    putOval(img, x, y + 1, shade(lips, 0.75), ocx, ocy, rx, ry)
  end
end

local function drawAce(img, suit, ink)
  local px, py = CX, CY - 8
  for i = 0, 17 do
    local a = i * math.pi / 9
    local lx = px + math.cos(a) * 102
    local ly = py + math.sin(a) * 118
    fillEllipse(img, lx, ly, 7, 4, (i % 2 == 0) and GOLD or GOLD_DIM)
  end
  strokeEllipse(img, px, py, 96, 112, 3, GOLD)
  drawSuitDetailed(img, suit, px, py, 148, ink, false)
  fillCircle(img, px, py + 8, 14, FACE_INNER)
  drawSuitDetailed(img, suit, px, py + 8, 18, ink, false)
end

local function drawCourt(img, suit, rank, ink)
  local ocx, ocy, orx, ory = CX, 262, 86, 118
  local hair = hairOf(suit)
  local clothes = shade(ink, 0.85)
  local trim = GOLD

  fillEllipse(img, ocx, ocy, orx, ory, shade(FACE_INNER, 0.92))
  fillEllipse(img, ocx, ocy, orx - 6, ory - 8, C(214, 198, 168))

  for y = 0, 78 do
    local w = 36 + math.floor(y * 0.55)
    fillRectOval(img, ocx - w, 292 + y, w * 2, 1, clothes, ocx, ocy, orx, ory)
    if y % 10 == 0 then
      fillRectOval(img, ocx - w, 292 + y, w * 2, 2, shade(clothes, 0.7), ocx, ocy, orx, ory)
    end
  end
  fillRectOval(img, ocx - 40, 300, 80, 8, trim, ocx, ocy, orx, ory)

  fillRectOval(img, ocx - 10, 268, 20, 28, SKIN, ocx, ocy, orx, ory)
  fillEllipseOval(img, ocx, 232, 40, 48, SKIN, ocx, ocy, orx, ory)
  fillEllipseOval(img, ocx - 36, 236, 7, 10, SKIN, ocx, ocy, orx, ory)
  fillEllipseOval(img, ocx + 36, 236, 7, 10, SKIN, ocx, ocy, orx, ory)
  fillEllipseOval(img, ocx + 18, 248, 7, 8, SKIN_D, ocx, ocy, orx, ory)
  fillEllipseOval(img, ocx - 12, 216, 11, 7, SKIN_L, ocx, ocy, orx, ory)
  for i = -8, 8 do
    putOval(img, ocx - 16 + i, 218, shade(ink, 0.55), ocx, ocy, orx, ory)
    putOval(img, ocx + 10 + i, 218, shade(ink, 0.55), ocx, ocy, orx, ory)
  end

  if rank == "J" then
    fillEllipseOval(img, ocx, 198, 46, 18, hair, ocx, ocy, orx, ory)
    fillRectOval(img, ocx - 46, 198, 92, 22, hair, ocx, ocy, orx, ory)
    fillRectOval(img, ocx - 52, 216, 104, 6, shade(hair, 0.65), ocx, ocy, orx, ory)
    fillRectOval(img, ocx - 44, 208, 88, 5, trim, ocx, ocy, orx, ory)
    fillEllipseOval(img, ocx + 50, 188, 6, 20, GOLD_LIGHT, ocx, ocy, orx, ory)
    fillSuit(img, suit, ocx, 206, 16, ink, false)
    drawEyes(img, ocx, 228, ink, ocx, ocy, orx, ory)
    drawNoseMouth(img, ocx, 228, ink, shade(ink, 0.55), ocx, ocy, orx, ory)
    fillEllipseOval(img, ocx, 318, 16, 14, ink, ocx, ocy, orx, ory)
    fillSuit(img, suit, ocx, 318, 18, FACE, false)
  elseif rank == "Q" then
    fillEllipseOval(img, ocx - 38, 250, 16, 40, hair, ocx, ocy, orx, ory)
    fillEllipseOval(img, ocx + 38, 250, 16, 40, hair, ocx, ocy, orx, ory)
    fillEllipseOval(img, ocx, 198, 44, 16, hair, ocx, ocy, orx, ory)
    fillRectOval(img, ocx - 36, 196, 72, 12, GOLD, ocx, ocy, orx, ory)
    for i = -2, 2 do
      fillDiamond(img, ocx + i * 15, 186, 5, 11, GOLD_LIGHT)
      fillCircle(img, ocx + i * 15, 176, 3, (i % 2 == 0) and ink or GOLD)
    end
    fillCircle(img, ocx - 28, 248, 3, GOLD_LIGHT)
    fillCircle(img, ocx + 28, 248, 3, GOLD_LIGHT)
    for i = -2, 2 do
      fillCircle(img, ocx + i * 8, 276, 3, GOLD)
    end
    drawEyes(img, ocx, 230, ink, ocx, ocy, orx, ory)
    drawNoseMouth(img, ocx, 230, ink, LIP, ocx, ocy, orx, ory)
    fillEllipseOval(img, ocx, 318, 16, 14, ink, ocx, ocy, orx, ory)
    fillSuit(img, suit, ocx, 318, 18, FACE, false)
  else
    fillEllipseOval(img, ocx, 200, 42, 14, hair, ocx, ocy, orx, ory)
    fillRectOval(img, ocx - 40, 188, 80, 14, GOLD, ocx, ocy, orx, ory)
    for i = -2, 2 do
      fillRectOval(img, ocx + i * 16 - 4, 172, 8, 20, GOLD_LIGHT, ocx, ocy, orx, ory)
      fillCircle(img, ocx + i * 16, 170, 3, ink)
    end
    fillEllipseOval(img, ocx, 258, 36, 26, hair, ocx, ocy, orx, ory)
    fillEllipseOval(img, ocx - 14, 244, 14, 6, hair, ocx, ocy, orx, ory)
    fillEllipseOval(img, ocx + 14, 244, 14, 6, hair, ocx, ocy, orx, ory)
    drawEyes(img, ocx, 226, ink, ocx, ocy, orx, ory)
    fillEllipseOval(img, ocx + 1, 236, 3, 6, SKIN_D, ocx, ocy, orx, ory)
    for i = 0, 10 do
      putOval(img, ocx - 5 + i, 248, SKIN, ocx, ocy, orx, ory)
    end
    for y = 0, 16 do
      local w = 28 - math.floor(y * 0.3)
      fillRectOval(img, ocx - w, 288 + y, w * 2, 1, WHITE, ocx, ocy, orx, ory)
      if y % 5 == 0 then
        fillCircle(img, ocx - 18, 292 + y, 2, ink)
        fillCircle(img, ocx + 18, 292 + y, 2, ink)
      end
    end
    fillEllipseOval(img, ocx, 328, 16, 14, ink, ocx, ocy, orx, ory)
    fillSuit(img, suit, ocx, 328, 18, FACE, false)
  end

  strokeEllipse(img, ocx, ocy, orx, ory, 5, GOLD)
  strokeEllipse(img, ocx, ocy, orx - 6, ory - 6, 2, GOLD_DIM)
  local tw = textWidth(rank, 3)
  stampText(img, rank, ocx - math.floor(tw / 2), 392, ink, 3, false)
end

local function drawTrump(img, suit, rank)
  local ink = suitInk(suit)
  drawFaceBase(img, ink)
  drawCornerIndex(img, rank, suit, ink)
  if rank == "A" then
    drawAce(img, suit, ink)
  elseif rank == "J" or rank == "Q" or rank == "K" then
    drawCourt(img, suit, rank, ink)
  else
    local layout = pipLayout(rank)
    for i = 1, #layout do
      local fx, fy, flip = layout[i][1], layout[i][2], layout[i][3]
      local px, py = mapPip(fx, fy)
      drawSuitDetailed(img, suit, px, py, 40, ink, flip)
    end
  end
end

local function inMainCircle(x, y, r)
  r = r or 120
  local dx, dy = x - CX, y - CY
  return dx * dx + dy * dy <= r * r
end

local function clipCover(x, y, r)
  r = r or 120
  local dx, dy = x + 0.5 - CX, y + 0.5 - CY
  return r + 0.5 - math.sqrt(dx * dx + dy * dy)
end

local function putInCircle(img, x, y, col, r)
  stampCover(img, x, y, col, clipCover(x, y, r or 120))
end

local function fillCircleClipped(img, cx, cy, rad, col, clipR)
  for y = math.floor(cy - rad - 1), math.ceil(cy + rad + 1) do
    for x = math.floor(cx - rad - 1), math.ceil(cx + rad + 1) do
      local dx, dy = x + 0.5 - cx, y + 0.5 - cy
      stampCover(img, x, y, col, math.min(rad + 0.5 - math.sqrt(dx * dx + dy * dy), clipCover(x, y, clipR)))
    end
  end
end

local function fillEllipseClipped(img, cx, cy, rx, ry, col, clipR)
  local k = math.min(rx, ry)
  for y = math.floor(cy - ry - 1), math.ceil(cy + ry + 1) do
    for x = math.floor(cx - rx - 1), math.ceil(cx + rx + 1) do
      local nx = (x + 0.5 - cx) / (rx + 0.0001)
      local ny = (y + 0.5 - cy) / (ry + 0.0001)
      local d = (math.sqrt(nx * nx + ny * ny) - 1) * k
      stampCover(img, x, y, col, math.min(0.5 - d, clipCover(x, y, clipR)))
    end
  end
end

local function grimPal(kind)
  if kind == "COLOR" then
    return {
      body = C(236, 86, 92),
      mid = C(176, 40, 48),
      shade = C(112, 18, 24),
      deep = C(48, 8, 12),
      ink = C(20, 4, 6),
      eye = C(255, 228, 228),
      tooth = C(248, 220, 214),
      gum = C(28, 6, 8),
    }
  end
  if kind == "MOON" then
    return {
      body = C(198, 220, 248),
      mid = C(92, 132, 196),
      shade = C(40, 68, 128),
      deep = C(16, 28, 64),
      ink = C(8, 12, 28),
      eye = C(236, 244, 255),
      tooth = C(220, 232, 248),
      gum = C(10, 16, 36),
    }
  end
  return {
    body = C(236, 236, 242),
    mid = C(168, 168, 178),
    shade = C(96, 96, 106),
    deep = C(32, 32, 38),
    ink = C(10, 10, 14),
    eye = C(250, 250, 252),
    tooth = C(236, 236, 240),
    gum = C(16, 16, 20),
  }
end

local function sdfCapsule(x, y, ax, ay, bx, by, r)
  local pax, pay = x - ax, y - ay
  local bax, bay = bx - ax, by - ay
  local den = bax * bax + bay * bay
  local h = 0
  if den > 0 then
    h = math.max(0, math.min(1, (pax * bax + pay * bay) / den))
  end
  local dx, dy = pax - bax * h, pay - bay * h
  return math.sqrt(dx * dx + dy * dy) - r
end

local function sdfEllipse(x, y, cx, cy, rx, ry)
  local nx = (x - cx) / rx
  local ny = (y - cy) / ry
  return (math.sqrt(nx * nx + ny * ny) - 1) * math.min(rx, ry)
end

local function moonSdf(x, y)
  local dOuter = math.sqrt((x - 156) * (x - 156) + (y - 278) * (y - 278)) - 96
  local dCut = math.sqrt((x - 216) * (x - 216) + (y - 246) * (y - 246)) - 80
  local d = math.max(dOuter, -dCut)
  d = math.min(d, sdfCapsule(x, y, 176, 256, 230, 270, 7.5))
  d = math.min(d, sdfCapsule(x, y, 220, 268, 228, 278, 4.2))
  local dMouth = sdfEllipse(x, y, 176, 316, 28, 16)
  if dMouth < 2 then
    d = math.max(d, -dMouth)
  end
  local dClip = math.sqrt((x - CX) * (x - CX) + (y - CY) * (y - CY)) - 119
  return math.max(d, dClip)
end

local function inMouth(x, y)
  return sdfEllipse(x + 0.5, y + 0.5, 176, 316, 28, 16) < 0
end

local function inGrimMoon(x, y)
  return moonSdf(x + 0.5, y + 0.5) < 0
end

local function drawLineAA(img, x0, y0, x1, y1, r, col, clipR)
  local minx = math.floor(math.min(x0, x1) - r - 1)
  local maxx = math.ceil(math.max(x0, x1) + r + 1)
  local miny = math.floor(math.min(y0, y1) - r - 1)
  local maxy = math.ceil(math.max(y0, y1) + r + 1)
  for y = miny, maxy do
    for x = minx, maxx do
      local d = sdfCapsule(x + 0.5, y + 0.5, x0, y0, x1, y1, r)
      stampCover(img, x, y, col, math.min(0.5 - d, clipCover(x, y, clipR)))
    end
  end
end

local function drawCrater(img, cx, cy, r, pal, clipR)
  for y = math.floor(cy - r - 1), math.ceil(cy + r + 1) do
    for x = math.floor(cx - r - 1), math.ceil(cx + r + 1) do
      if inGrimMoon(x, y) then
        local dx, dy = x + 0.5 - cx, y + 0.5 - cy
        local t = math.sqrt(dx * dx + dy * dy) / r
        if t < 1.15 then
          local col = lerpCol(pal.deep, pal.mid, math.max(0, math.min(1, (t - 0.15) / 0.7)))
          if t > 0.72 then
            col = lerpCol(pal.mid, pal.body, (t - 0.72) / 0.35)
          end
          stampCover(img, x, y, col, math.min(1.1 - t, clipCover(x, y, clipR)))
        end
      end
    end
  end
end

local function drawGrimMoon(img, kind, clipR)
  local pal = grimPal(kind)
  for y = CY - clipR - 1, CY + clipR + 1 do
    for x = CX - clipR - 1, CX + clipR + 1 do
      local d = moonSdf(x + 0.5, y + 0.5)
      local a = 0.5 - d
      if a > 0 then
        local t = math.max(0, math.min(1, (x - 118) / 100))
        local u = math.max(0, math.min(1, (y - 200) / 140))
        local col = lerpCol(pal.mid, pal.body, t)
        if t < 0.28 then
          col = lerpCol(pal.shade, pal.mid, t / 0.28)
        end
        if t > 0.62 and u < 0.42 then
          col = lerpCol(col, pal.eye, (t - 0.62) * 0.55)
        end
        if d > -1.6 then
          col = lerpCol(pal.ink, col, math.max(0, -d) / 1.6)
        end
        stampCover(img, x, y, col, math.min(a, clipCover(x, y, clipR)))
      end
    end
  end

  for y = 296, 338 do
    for x = 146, 208 do
      local md = sdfEllipse(x + 0.5, y + 0.5, 176, 316, 28, 16)
      if md < 0.8 then
        stampCover(img, x, y, pal.gum, math.min(0.5 - md, clipCover(x, y, clipR)))
      end
    end
  end

  for i = 0, 6 do
    local tx = 154 + i * 7.2
    local ty = 306 + math.abs(i - 3) * 1.4
    fillEllipseClipped(img, tx, ty, 3.1, 5.2, pal.tooth, clipR)
    fillEllipseClipped(img, tx + 1, ty + 11, 3.0, 4.6, pal.tooth, clipR)
  end

  local ex, ey, er = 148, 242, 17
  fillCircleClipped(img, ex, ey, er + 2.2, pal.ink, clipR)
  fillCircleClipped(img, ex, ey, er, pal.eye, clipR)
  strokeCircle(img, ex, ey, er, 1.6, pal.ink)
  fillCircleClipped(img, ex + 1, ey + 1, 2.4, pal.ink, clipR)
  fillCircleClipped(img, ex - 4, ey - 5, 1.6, WHITE, clipR)
  for a = 0, 9 do
    local ang = a * math.pi / 5 + 0.2
    drawLineAA(img,
      ex + math.cos(ang) * (er + 1),
      ey + math.sin(ang) * (er + 1),
      ex + math.cos(ang) * (er + 11),
      ey + math.sin(ang) * (er + 11),
      0.7, pal.ink, clipR)
  end

  fillCircleClipped(img, 214, 274, 2.2, pal.deep, clipR)

  drawCrater(img, 136, 300, 10, pal, clipR)
  drawCrater(img, 148, 214, 7, pal, clipR)
  drawCrater(img, 130, 258, 6, pal, clipR)
  drawCrater(img, 144, 334, 8, pal, clipR)
  drawCrater(img, 166, 206, 5, pal, clipR)
  drawCrater(img, 132, 322, 5, pal, clipR)

  drawLineAA(img, 134, 268, 146, 308, 0.65, pal.ink, clipR)
  drawLineAA(img, 144, 332, 166, 350, 0.65, pal.ink, clipR)
  drawLineAA(img, 166, 202, 150, 224, 0.65, pal.ink, clipR)

  if kind == "COLOR" then
    drawLineAA(img, 196, 322, 206, 372, 2.4, BLOOD, clipR)
    fillCircleClipped(img, 200, 336, 5.5, BLOOD, clipR)
    fillCircleClipped(img, 204, 356, 4.5, BLOOD_DARK, clipR)
    fillCircleClipped(img, 207, 372, 3.2, BLOOD, clipR)
    fillEllipseClipped(img, 194, 320, 6, 3.5, BLOOD_LIGHT, clipR)
  end
end

local function drawJokerStars(img, clipR)
  local seed = 17
  for i = 1, 36 do
    seed = (seed * 1103515245 + 12345) % 2147483647
    local ang = (seed % 360) * math.pi / 180
    seed = (seed * 1103515245 + 12345) % 2147483647
    local dist = 18 + (seed % 96)
    local x = CX + math.cos(ang) * dist
    local y = CY + math.sin(ang) * dist
    if clipCover(math.floor(x), math.floor(y), 108) > 0 and moonSdf(x, y) > 4 then
      seed = (seed * 1103515245 + 12345) % 2147483647
      local col = (seed % 3 == 0) and C(255, 232, 168) or C(226, 236, 255)
      fillCircleClipped(img, x, y, 1.1 + (seed % 2) * 0.6, col, clipR)
    end
  end
end

local function drawJokerCircleChrome(img, fill, ring)
  fillCircle(img, CX, CY, 120, fill)
  strokeCircle(img, CX, CY, 120, 4, ring)
  strokeCircle(img, CX, CY, 114, 1.6, GOLD_DIM)
end

local function drawJoker(img, kind)
  local clipR = 120
  if kind == "BW" then
    drawFaceBase(img, INK_BLACK)
    drawJokerCircleChrome(img, C(12, 12, 16), C(196, 196, 204))
    drawGrimMoon(img, "BW", clipR)
  elseif kind == "COLOR" then
    drawFaceBase(img, JOKER_RED)
    drawJokerCircleChrome(img, C(36, 8, 12), C(220, 64, 72))
    drawGrimMoon(img, "COLOR", clipR)
  else
    drawFaceBase(img, JOKER_BLUE)
    fillCircle(img, CX, CY, 120, C(8, 14, 38))
    strokeCircle(img, CX, CY, 120, 4, C(120, 170, 255))
    strokeCircle(img, CX, CY, 114, 1.6, GOLD_DIM)
    drawJokerStars(img, clipR)
    drawGrimMoon(img, "MOON", clipR)
    drawJokerStars(img, clipR)
  end
end

local function drawCaption(img, text, col)
  local scale = 3
  local tw = textWidth(text, scale)
  stampText(img, text, math.floor((W - tw) / 2), H - 52, col, scale, false)
end

local function drawSpearIcon(img)
  fillDiamond(img, CX, 156, 20, 40, STEEL_D)
  fillDiamond(img, CX, 152, 14, 32, STEEL)
  fillDiamond(img, CX - 2, 146, 6, 20, STEEL_L)
  fillRect(img, CX - 1, 150, 3, 42, STEEL_D)
  fillRect(img, CX - 16, 188, 32, 16, GOLD)
  fillRect(img, CX - 12, 192, 24, 8, GOLD_LIGHT)
  fillRect(img, CX - 6, 200, 12, 196, WOOD)
  fillRect(img, CX - 3, 204, 3, 188, WOOD_L)
  fillRect(img, CX + 2, 210, 2, 176, WOOD_D)
  for i = 0, 5 do
    fillRect(img, CX - 8, 230 + i * 28, 16, 5, shade(WOOD, 0.7))
  end
  fillEllipse(img, CX + 18, 214, 10, 16, INK_RED)
  fillEllipse(img, CX + 22, 226, 6, 12, shade(INK_RED, 0.7))
  fillRect(img, CX - 14, 396, 28, 10, STEEL)
  fillRect(img, CX - 8, 404, 16, 8, STEEL_D)
end

local function drawPassIcon(img, col)
  strokeCircle(img, CX, CY - 8, 76, 8, col)
  strokeCircle(img, CX, CY - 8, 64, 2, shade(col, 1.4))
  fillPoly(img, { { CX - 6, CY - 54 }, { CX + 50, CY - 8 }, { CX - 6, CY + 38 } }, col)
  fillPoly(img, { { CX - 44, CY - 40 }, { CX - 4, CY - 8 }, { CX - 44, CY + 24 } }, col)
  fillPoly(img, { { CX - 2, CY - 40 }, { CX + 28, CY - 8 }, { CX - 2, CY + 24 } }, FACE_INNER)
  for i = 0, 4 do
    local a = -0.8 + i * 0.18
    local x = CX + 70 + math.floor(math.cos(a) * 8)
    local y = CY - 40 + i * 10
    fillRect(img, x, y, 10, 2, shade(col, 0.7))
  end
end

local function drawRevIcon(img, col)
  strokeCircle(img, CX, CY - 6, 68, 8, col)
  strokeCircle(img, CX, CY - 6, 56, 2, GOLD_DIM)
  fillPoly(img, { { CX + 50, CY - 46 }, { CX + 86, CY - 8 }, { CX + 46, CY - 2 } }, col)
  fillPoly(img, { { CX - 50, CY + 34 }, { CX - 86, CY - 4 }, { CX - 46, CY - 10 } }, col)
  fillMoonSuit(img, CX, CY - 6, 36, col, false)
end

local function drawCounterIcon(img)
  local function blade(x0, y0, x1, y1, wide)
    for t = 0, 160 do
      local u = t / 160
      local x = math.floor(x0 + (x1 - x0) * u)
      local y = math.floor(y0 + (y1 - y0) * u)
      local w = wide + math.floor((1 - math.abs(u - 0.45)) * 4)
      for i = -w, w do
        put(img, x + i, y, (i == -w or i == w) and STEEL_D or (i < 0 and STEEL_L or STEEL))
      end
    end
  end
  blade(CX - 78, CY - 78, CX + 78, CY + 78, 5)
  blade(CX + 78, CY - 78, CX - 78, CY + 78, 4)
  fillRect(img, CX - 88, CY - 88, 22, 14, GOLD)
  fillRect(img, CX + 66, CY + 74, 22, 14, GOLD)
  fillRect(img, CX + 66, CY - 88, 22, 14, GOLD_DIM)
  fillRect(img, CX - 88, CY + 74, 22, 14, GOLD_DIM)
end

local function drawMirrorIcon(img)
  fillEllipse(img, CX, CY - 18, 70, 96, INK_BLUE)
  fillEllipse(img, CX, CY - 18, 58, 84, C(210, 226, 240))
  fillEllipse(img, CX - 16, CY - 40, 14, 48, C(236, 244, 252))
  fillEllipse(img, CX + 18, CY - 8, 8, 28, C(186, 204, 226))
  fillEllipse(img, CX + 4, CY + 10, 10, 22, shade(INK_BLUE, 1.6))
  fillDiamond(img, CX, CY - 122, 10, 16, GOLD)
  fillRect(img, CX - 18, CY - 118, 36, 8, GOLD)
  fillRect(img, CX - 22, CY + 76, 44, 10, GOLD)
  fillRect(img, CX - 36, CY + 84, 16, 18, GOLD_DIM)
  fillRect(img, CX + 20, CY + 84, 16, 18, GOLD_DIM)
  strokeEllipse(img, CX, CY - 18, 70, 96, 4, GOLD)
end

local function drawPillIcon(img, col)
  fillEllipse(img, CX - 46, CY - 6, 40, 30, col)
  fillRect(img, CX - 46, CY - 36, 46, 60, col)
  fillEllipse(img, CX - 30, CY - 18, 12, 8, shade(col, 1.45))
  fillEllipse(img, CX + 46, CY - 6, 40, 30, C(250, 248, 240))
  fillRect(img, CX, CY - 36, 46, 60, C(250, 248, 240))
  fillEllipse(img, CX + 28, CY - 18, 10, 6, WHITE)
  fillRect(img, CX - 3, CY - 36, 6, 60, shade(col, 0.7))
  strokeEllipse(img, CX - 46, CY - 6, 40, 30, 2, shade(col, 0.55))
  strokeEllipse(img, CX + 46, CY - 6, 40, 30, 2, shade(col, 0.55))
  for i = 0, 5 do
    put(img, CX - 20 + (i % 3) * 8, CY + 8 + math.floor(i / 3) * 8, shade(col, 1.25))
  end
end

local function drawSpecial(img, id)
  if id == "JOKER:COLOR" then
    drawJoker(img, "COLOR")
  elseif id == "JOKER:BW" then
    drawJoker(img, "BW")
  elseif id == "JOKER:MOON" then
    drawJoker(img, "MOON")
  elseif id == "SPEC:SPEAR" then
    drawFaceBase(img, SPEC_BORDER)
    drawSpearIcon(img)
    drawCaption(img, "SPEAR", INK_BLACK)
  elseif id == "SPEC:PASS" then
    drawFaceBase(img, SPEC_BORDER)
    drawPassIcon(img, INK_BLACK)
    drawCaption(img, "PASS", INK_BLACK)
  elseif id == "SPEC:REVJOKER" then
    drawFaceBase(img, SPEC_BORDER)
    drawRevIcon(img, INK_BLACK)
    drawCaption(img, "REV", INK_BLACK)
  elseif id == "SPEC:COUNTER" then
    drawFaceBase(img, SPEC_BORDER)
    drawCounterIcon(img)
    drawCaption(img, "CTR", INK_BLACK)
  elseif id == "SPEC:MIRROR" then
    drawFaceBase(img, SPEC_BORDER)
    drawMirrorIcon(img)
    drawCaption(img, "MIRROR", INK_BLUE)
  elseif id == "SPEC:PILL_BK" then
    drawFaceBase(img, PILL_BK)
    drawPillIcon(img, PILL_BK)
    drawCaption(img, "PILL BK", PILL_BK)
  elseif id == "SPEC:PILL_RD" then
    drawFaceBase(img, PILL_RD)
    drawPillIcon(img, PILL_RD)
    drawCaption(img, "PILL RD", PILL_RD)
  elseif id == "SPEC:PILL_BL" then
    drawFaceBase(img, PILL_BL)
    drawPillIcon(img, PILL_BL)
    drawCaption(img, "PILL BL", PILL_BL)
  end
end

local function drawBack(img)
  fillRoundRect(img, NAVY_DARK)
  for y = 32, H - 33, 28 do
    for x = 32, W - 33, 28 do
      fillCircle(img, x + 10, y + 10, 2.2, NAVY_MID)
    end
  end
  strokeRoundRect(img, 4, 5, GOLD)
  strokeRoundRect(img, 14, 2, NAVY_LIGHT)
  fillCircle(img, CX, CY, 82, NAVY)
  strokeCircle(img, CX, CY, 82, 5, GOLD)
  strokeCircle(img, CX, CY, 73, 1.6, GOLD_DIM)
  fillStar(img, CX, CY - 54, 8, 3, GOLD_LIGHT)
  fillMoonSuit(img, CX, CY + 54, 22, GOLD, false)
  local tw = textWidth("OT", 6)
  stampText(img, "OT", CX - math.floor(tw / 2), CY - 28, GOLD, 6, false)
end

local function fileName(defId)
  return defId:gsub(":", "_") .. ".png"
end

local suits = { "S", "H", "D", "C", "R", "M" }
local ranks = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" }
local specials = {
  "JOKER:COLOR", "JOKER:BW", "JOKER:MOON",
  "SPEC:SPEAR", "SPEC:PASS", "SPEC:REVJOKER", "SPEC:COUNTER", "SPEC:MIRROR",
  "SPEC:PILL_BK", "SPEC:PILL_RD", "SPEC:PILL_BL",
}

local cards = {}
for _, s in ipairs(suits) do
  for _, r in ipairs(ranks) do
    cards[#cards + 1] = { id = s .. r, kind = "trump", suit = s, rank = r }
  end
end
for _, id in ipairs(specials) do
  cards[#cards + 1] = { id = id, kind = "spec" }
end

local spr = Sprite(W, H, ColorMode.RGB)
spr.filename = SRC
app.activeSprite = spr

local bgLayer = spr.layers[1]
bgLayer.name = "Background"

while #spr.frames < (#cards + 1) do
  spr:newFrame()
end

local function paintCel(layer, frame, painter)
  local cel = layer:cel(frame) or spr:newCel(layer, frame)
  local img = Image(W, H, ColorMode.RGB)
  painter(img)
  cel.image = img
  cel.position = Point(0, 0)
end

for i, card in ipairs(cards) do
  local frame = spr.frames[i]
  local composed = Image(W, H, ColorMode.RGB)
  if card.kind == "trump" then
    paintCel(bgLayer, frame, function(img)
      drawTrump(img, card.suit, card.rank)
    end)
    drawTrump(composed, card.suit, card.rank)
  else
    paintCel(bgLayer, frame, function(img)
      drawSpecial(img, card.id)
    end)
    drawSpecial(composed, card.id)
  end
  composed:saveAs(OUT .. "/" .. fileName(card.id))
end

local backFrame = spr.frames[#cards + 1]
paintCel(bgLayer, backFrame, function(img)
  drawBack(img)
end)
local backImg = Image(W, H, ColorMode.RGB)
drawBack(backImg)
backImg:saveAs(OUT .. "/BACK.png")

spr:saveAs(SRC)
print("exported " .. #cards .. " fronts + BACK at " .. W .. "x" .. H)