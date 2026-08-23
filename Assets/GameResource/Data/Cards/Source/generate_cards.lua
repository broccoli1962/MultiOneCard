-- OneTable Official 89 CardDefId fronts + BACK at native 384x540 (32:45).
-- Shaded pips, court portraits, detailed jokers/specials. Not a 64x90 upscale.

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

local function fillRect(img, x, y, w, h, col)
  for iy = y, y + h - 1 do
    for ix = x, x + w - 1 do
      put(img, ix, iy, col)
    end
  end
end

local function inRoundRect(x, y, w, h, rad)
  if x < 0 or y < 0 or x >= w or y >= h then
    return false
  end
  if x >= rad and x < w - rad then
    return true
  end
  if y >= rad and y < h - rad then
    return true
  end
  local cx = x < rad and rad or (w - 1 - rad)
  local cy = y < rad and rad or (h - 1 - rad)
  local dx, dy = x - cx, y - cy
  return dx * dx + dy * dy <= rad * rad
end

local function fillRoundRect(img, col)
  for y = 0, H - 1 do
    for x = 0, W - 1 do
      if inRoundRect(x, y, W, H, RAD) then
        put(img, x, y, col)
      end
    end
  end
end

local function strokeRoundRect(img, inset, thick, col)
  local rw, rh = W - inset * 2, H - inset * 2
  local rad = math.max(4, RAD - inset)
  for y = 0, rh - 1 do
    for x = 0, rw - 1 do
      if inRoundRect(x, y, rw, rh, rad) then
        local inner = inRoundRect(x - thick, y - thick, rw - thick * 2, rh - thick * 2, math.max(2, rad - thick))
        if not inner then
          put(img, x + inset, y + inset, col)
        end
      end
    end
  end
end

local function fillCircle(img, cx, cy, r, col)
  local r2 = r * r
  for y = cy - r, cy + r do
    for x = cx - r, cx + r do
      local dx, dy = x - cx, y - cy
      if dx * dx + dy * dy <= r2 then
        put(img, x, y, col)
      end
    end
  end
end

local function fillEllipse(img, cx, cy, rx, ry, col)
  local rx2, ry2 = rx * rx, ry * ry
  for y = cy - ry, cy + ry do
    for x = cx - rx, cx + rx do
      local dx, dy = x - cx, y - cy
      if dx * dx * ry2 + dy * dy * rx2 <= rx2 * ry2 then
        put(img, x, y, col)
      end
    end
  end
end

local function strokeCircle(img, cx, cy, r, thick, col)
  local r2 = r * r
  local inner = math.max(0, r - thick)
  local i2 = inner * inner
  for y = cy - r, cy + r do
    for x = cx - r, cx + r do
      local dx, dy = x - cx, y - cy
      local d2 = dx * dx + dy * dy
      if d2 <= r2 and d2 >= i2 then
        put(img, x, y, col)
      end
    end
  end
end

local function strokeEllipse(img, cx, cy, rx, ry, thick, col)
  local rx2, ry2 = rx * rx, ry * ry
  local irx, iry = math.max(1, rx - thick), math.max(1, ry - thick)
  local irx2, iry2 = irx * irx, iry * iry
  for y = cy - ry, cy + ry do
    for x = cx - rx, cx + rx do
      local dx, dy = x - cx, y - cy
      if dx * dx * ry2 + dy * dy * rx2 <= rx2 * ry2 then
        if dx * dx * iry2 + dy * dy * irx2 > irx2 * iry2 then
          put(img, x, y, col)
        end
      end
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
  for y = cy - hh, cy + hh do
    local t = 1 - math.abs(y - cy) / math.max(1, hh)
    local w = math.floor(hw * t)
    for x = cx - w, cx + w do
      put(img, x, y, col)
    end
  end
end

local function fillHeart(img, cx, cy, s, col, flip)
  local r = math.max(4, math.floor(s * 0.26))
  local oy = flip and 1 or -1
  local top = cy + oy * math.floor(s * 0.12)
  fillCircle(img, cx - r, top, r, col)
  fillCircle(img, cx + r, top, r, col)
  local y0 = cy + oy * math.floor(s * 0.02)
  local y1 = cy - oy * math.floor(s * 0.50)
  local step = y0 < y1 and 1 or -1
  local half = math.floor(s * 0.52)
  for y = y0, y1, step do
    local t = math.abs(y - y0) / math.max(1, math.abs(y1 - y0))
    local w = math.floor(half * (1 - t))
    for x = cx - w, cx + w do
      put(img, x, y, col)
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
  local r = math.floor(s * 0.46)
  local ox = cx + math.floor(s * 0.16)
  local oy = cy + (flip and 4 or -4)
  local r2 = math.floor(s * 0.38)
  local rsq, r2sq = r * r, r2 * r2
  for y = cy - r, cy + r do
    for x = cx - r, cx + r do
      local dx, dy = x - cx, y - cy
      if dx * dx + dy * dy <= rsq then
        local dx2, dy2 = x - ox, y - oy
        if dx2 * dx2 + dy2 * dy2 > r2sq then
          put(img, x, y, col)
        end
      end
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
  local hi, lo = shade(col, 1.26), shade(col, 0.72)
  for y = cy - reach, cy + reach do
    for x = cx - reach, cx + reach do
      if x >= 0 and y >= 0 and x < W and y < H and sameRgb(img:getPixel(x, y), col) then
        local s = (x - cx) + (y - cy)
        if s < -reach * 0.16 then
          put(img, x, y, hi)
        elseif s > reach * 0.20 then
          put(img, x, y, lo)
        end
      end
    end
  end
end

local function drawSuitDetailed(img, suit, cx, cy, size, col, flip)
  fillSuit(img, suit, cx + 1, cy + 2, size + 2, shade(col, 0.42), flip)
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

local function applyPaperGrain(img)
  for y = 0, H - 1 do
    for x = 0, W - 1 do
      local n = (x * 131 + y * 313 + x * y * 7) % 13
      if n <= 1 then
        local c = Color(img:getPixel(x, y))
        if c.alpha > 0 then
          local f = n == 0 and 0.965 or 1.035
          put(img, x, y, C(clamp8(c.red * f), clamp8(c.green * f), clamp8(c.blue * f), c.alpha))
        end
      end
    end
  end
end

local function drawFiligree(img, border)
  local function arm(x, y, sx, sy)
    for i = 0, 28 do
      put(img, x + i * sx, y, GOLD)
      put(img, x, y + i * sy, GOLD)
    end
    fillDiamond(img, x + 20 * sx, y, 3, 4, GOLD_LIGHT)
    fillDiamond(img, x, y + 20 * sy, 3, 4, GOLD_LIGHT)
    fillCircle(img, x + 7 * sx, y + 7 * sy, 2, border)
    put(img, x + 12 * sx, y + 4 * sy, GOLD_DIM)
    put(img, x + 4 * sx, y + 12 * sy, GOLD_DIM)
  end
  arm(26, 26, 1, 1)
  arm(W - 27, 26, -1, 1)
  arm(26, H - 27, 1, -1)
  arm(W - 27, H - 27, -1, -1)
end

local function drawFaceBase(img, border)
  fillRoundRect(img, FACE)
  fillRect(img, 28, 36, W - 56, H - 72, FACE_INNER)
  strokeRoundRect(img, 4, 5, border)
  strokeRoundRect(img, 12, 3, GOLD)
  strokeRoundRect(img, 18, 1, GOLD_DIM)
  drawFiligree(img, border)
  applyPaperGrain(img)
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
  if inOval(x + 0.5, y + 0.5, ocx, ocy, rx, ry) then
    put(img, x, y, col)
  end
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

local function putInCircle(img, x, y, col, r)
  if inMainCircle(x, y, r or 120) then
    put(img, x, y, col)
  end
end

local function fillCircleClipped(img, cx, cy, rad, col, clipR)
  local r2 = rad * rad
  for y = cy - rad, cy + rad do
    for x = cx - rad, cx + rad do
      local dx, dy = x - cx, y - cy
      if dx * dx + dy * dy <= r2 then
        putInCircle(img, x, y, col, clipR)
      end
    end
  end
end

local function fillEllipseClipped(img, cx, cy, rx, ry, col, clipR)
  local rx2, ry2 = rx * rx, ry * ry
  for y = cy - ry, cy + ry do
    for x = cx - rx, cx + rx do
      local dx, dy = x - cx, y - cy
      if dx * dx * ry2 + dy * dy * rx2 <= rx2 * ry2 then
        putInCircle(img, x, y, col, clipR)
      end
    end
  end
end

local function drawCrescentMoon(img, body, shadeCol, clipR)
  local mx, my, mr = 168, 274, 80
  local sx, sy, sr = 204, 252, 70
  local mrsq, srsq = mr * mr, sr * sr
  for y = my - mr, my + mr do
    for x = mx - mr, mx + mr do
      local dx, dy = x - mx, y - my
      if dx * dx + dy * dy <= mrsq then
        local dx2, dy2 = x - sx, y - sy
        if dx2 * dx2 + dy2 * dy2 > srsq then
          local t = (x - (mx - mr)) / (mr * 2)
          local u = (y - (my - mr)) / (mr * 2)
          local col = body
          if t < 0.28 then
            col = shadeCol
          elseif t > 0.72 and u < 0.45 then
            col = lerpCol(body, WHITE, 0.22)
          end
          putInCircle(img, x, y, col, clipR)
        end
      end
    end
  end
  fillCircleClipped(img, 148, 292, 9, shade(shadeCol, 0.88), clipR)
  fillCircleClipped(img, 172, 248, 6, shade(shadeCol, 0.9), clipR)
  fillCircleClipped(img, 156, 262, 4, shade(shadeCol, 0.8), clipR)
  fillCircleClipped(img, 146, 292, 3, lerpCol(shadeCol, body, 0.4), clipR)
end

local function drawMoonFace(img, eyeWhite, pupil, mouth, clipR)
  fillEllipseClipped(img, 154, 246, 7, 6, shade(pupil, 1.8), clipR)
  fillEllipseClipped(img, 150, 276, 6, 5, shade(pupil, 1.8), clipR)
  fillEllipseClipped(img, 156, 248, 9, 11, eyeWhite, clipR)
  fillEllipseClipped(img, 152, 278, 8, 10, eyeWhite, clipR)
  fillCircleClipped(img, 158, 250, 4, pupil, clipR)
  fillCircleClipped(img, 154, 280, 3, pupil, clipR)
  putInCircle(img, 160, 247, WHITE, clipR)
  putInCircle(img, 156, 277, WHITE, clipR)
  for i = -6, 6 do
    putInCircle(img, 154 + i, 238, shade(pupil, 1.4), clipR)
    putInCircle(img, 150 + i, 268, shade(pupil, 1.4), clipR)
  end
  fillEllipseClipped(img, 168, 268, 4, 5, shade(pupil, 1.6), clipR)
  for i = 0, 26 do
    local t = i / 26
    local x = 146 + math.floor(t * 32)
    local y = 300 + math.floor(math.sin(t * math.pi) * 9)
    putInCircle(img, x, y, mouth, clipR)
    putInCircle(img, x, y + 1, mouth, clipR)
    putInCircle(img, x, y + 2, shade(mouth, 0.7), clipR)
  end
end

local function drawBlood(img, clipR)
  fillEllipseClipped(img, 174, 308, 8, 5, BLOOD, clipR)
  for i = 0, 58 do
    local x = 174 + math.floor(math.sin(i * 0.18) * 2 + i * 0.16)
    local y = 308 + i
    local r = (i < 10) and 4 or (i < 30 and 5 or (i < 46 and 4 or 3))
    fillCircleClipped(img, x, y, r, (i % 6 == 0) and BLOOD_DARK or BLOOD, clipR)
    if i % 11 == 0 then
      fillCircleClipped(img, x + 5, y + 2, 2, BLOOD_LIGHT, clipR)
    end
  end
  fillCircleClipped(img, 178, 326, 7, BLOOD, clipR)
  fillCircleClipped(img, 182, 348, 6, BLOOD_DARK, clipR)
  fillCircleClipped(img, 186, 366, 5, BLOOD, clipR)
  fillCircleClipped(img, 188, 380, 3, BLOOD_LIGHT, clipR)
  fillEllipseClipped(img, 170, 312, 8, 4, BLOOD_LIGHT, clipR)
end

local function drawJokerCircleChrome(img, fill, ring)
  fillCircle(img, CX, CY, 120, fill)
  strokeCircle(img, CX, CY, 120, 5, ring)
  strokeCircle(img, CX, CY, 114, 2, GOLD_DIM)
  for i = 0, 23 do
    local a = i * math.pi / 12
    local x = CX + math.floor(math.cos(a) * 108)
    local y = CY + math.floor(math.sin(a) * 108)
    fillCircle(img, x, y, (i % 3 == 0) and 2 or 1, GOLD_LIGHT)
  end
end

local function drawJoker(img, kind)
  local clipR = 120
  if kind == "BW" then
    drawFaceBase(img, INK_BLACK)
    drawJokerCircleChrome(img, C(16, 16, 20), C(200, 200, 208))
    drawCrescentMoon(img, MOON_FILL, MOON_SHADE, clipR)
    drawMoonFace(img, C(248, 248, 252), MOON_INK, C(48, 48, 54), clipR)
  elseif kind == "COLOR" then
    drawFaceBase(img, JOKER_RED)
    drawJokerCircleChrome(img, C(42, 10, 14), C(220, 64, 72))
    drawCrescentMoon(img, CRESCENT_RED, CRESCENT_RED_SHADE, clipR)
    drawMoonFace(img, C(255, 220, 220), C(80, 12, 16), C(90, 10, 16), clipR)
    drawBlood(img, clipR)
  else
    drawFaceBase(img, JOKER_BLUE)
    for r = 120, 16, -10 do
      local t = 1 - r / 120
      fillCircle(img, CX, CY, r, lerpCol(C(8, 14, 40), C(28, 52, 110), t * 0.55))
    end
    strokeCircle(img, CX, CY, 120, 5, C(120, 170, 255))
    strokeCircle(img, CX, CY, 114, 2, GOLD_DIM)
    local seed = 17
    for i = 1, 72 do
      seed = (seed * 1103515245 + 12345) % 2147483647
      local ang = (seed % 360) * math.pi / 180
      seed = (seed * 1103515245 + 12345) % 2147483647
      local dist = 10 + (seed % 100)
      local x = CX + math.floor(math.cos(ang) * dist)
      local y = CY + math.floor(math.sin(ang) * dist)
      if inMainCircle(x, y, 108) then
        seed = (seed * 1103515245 + 12345) % 2147483647
        local kindStar = seed % 6
        local col = (kindStar == 0) and C(255, 232, 160) or (kindStar == 1 and C(200, 220, 255) or C(240, 246, 255))
        if kindStar >= 4 then
          fillStar(img, x, y, 5 + (seed % 5), 2, col)
        else
          fillSparkle(img, x, y, 2 + (seed % 4), col)
        end
      end
    end
    local lines = {
      { CX - 40, CY - 30, CX - 8, CY - 48 },
      { CX - 8, CY - 48, CX + 22, CY - 20 },
      { CX + 22, CY - 20, CX + 8, CY + 18 },
      { CX - 24, CY + 36, CX + 8, CY + 18 },
    }
    for i = 1, #lines do
      local x0, y0, x1, y1 = lines[i][1], lines[i][2], lines[i][3], lines[i][4]
      for t = 0, 20 do
        local u = t / 20
        putInCircle(img, math.floor(x0 + (x1 - x0) * u), math.floor(y0 + (y1 - y0) * u), C(160, 190, 230), clipR)
      end
    end
    fillSparkle(img, CX - 36, CY - 28, 7, C(255, 248, 220))
    fillSparkle(img, CX + 40, CY + 18, 6, WHITE)
    fillStar(img, CX + 8, CY - 44, 10, 4, C(210, 230, 255))
    fillStar(img, CX - 20, CY + 40, 8, 3, C(255, 236, 170))
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
  for y = 26, H - 27, 18 do
    for x = 26, W - 27, 18 do
      local alt = ((x / 18) + (y / 18)) % 2 < 1
      if alt then
        fillStar(img, x + 8, y + 8, 6, 2, NAVY_LIGHT)
      else
        fillMoonSuit(img, x + 8, y + 8, 13, NAVY_MID, false)
      end
    end
  end
  strokeRoundRect(img, 4, 6, GOLD)
  strokeRoundRect(img, 14, 3, NAVY_LIGHT)
  strokeRoundRect(img, 20, 1, GOLD_DIM)
  local function arm(x, y, sx, sy)
    for i = 0, 22 do
      put(img, x + i * sx, y, GOLD)
      put(img, x, y + i * sy, GOLD)
    end
    fillStar(img, x + 10 * sx, y + 10 * sy, 5, 2, GOLD_LIGHT)
  end
  arm(28, 28, 1, 1)
  arm(W - 29, 28, -1, 1)
  arm(28, H - 29, 1, -1)
  arm(W - 29, H - 29, -1, -1)
  fillCircle(img, CX, CY, 84, NAVY)
  strokeCircle(img, CX, CY, 84, 6, GOLD)
  strokeCircle(img, CX, CY, 74, 2, GOLD_DIM)
  fillStar(img, CX, CY - 58, 8, 3, GOLD_LIGHT)
  fillMoonSuit(img, CX, CY + 58, 22, GOLD, false)
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