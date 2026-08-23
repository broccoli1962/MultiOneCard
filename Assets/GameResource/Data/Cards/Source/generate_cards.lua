-- OneTable Official 89 CardDefId fronts + BACK at native 384x540 (32:45).
-- Not a 64x90 nearest-neighbor upscale: pip layouts, filled suits, joker circles.

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
local NAVY = C(24, 32, 58)
local NAVY_DARK = C(14, 18, 36)
local NAVY_LIGHT = C(42, 56, 96)
local NAVY_MID = C(30, 42, 78)
local SPEC_BORDER = C(92, 72, 36)
local WHITE = C(255, 255, 255)
local GRAY = C(90, 90, 96)
local PILL_BK = C(36, 36, 40)
local PILL_RD = C(200, 48, 54)
local PILL_BL = C(40, 86, 196)
local JOKER_RED = C(200, 40, 48)
local JOKER_BLUE = C(40, 86, 196)
local BLOOD = C(176, 16, 28)
local BLOOD_DARK = C(120, 8, 16)
local MOON_FILL = C(232, 232, 240)
local MOON_SHADE = C(176, 176, 188)
local MOON_INK = C(36, 36, 42)
local CRESCENT_RED = C(232, 72, 80)
local CRESCENT_RED_SHADE = C(160, 28, 36)

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

local function drawFaceBase(img, border)
  fillRoundRect(img, FACE)
  fillRect(img, 28, 36, W - 56, H - 72, FACE_INNER)
  strokeRoundRect(img, 4, 5, border)
  strokeRoundRect(img, 12, 3, GOLD)
end

local function drawCornerIndex(img, rank, suit, ink)
  local scale = 4
  stampText(img, rank, 22, 20, ink, scale, false)
  fillSuit(img, suit, 36, 78, 28, ink, false)
  local tw = textWidth(rank, scale)
  local rh = 9 * scale
  stampText(img, rank, W - 22 - tw, H - 20 - rh, ink, scale, true)
  fillSuit(img, suit, W - 36, H - 78, 28, ink, true)
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

local function drawTrump(img, suit, rank)
  local ink = suitInk(suit)
  drawFaceBase(img, ink)
  drawCornerIndex(img, rank, suit, ink)
  local layout = pipLayout(rank)
  for i = 1, #layout do
    local fx, fy, flip, kind = layout[i][1], layout[i][2], layout[i][3], layout[i][4]
    local px, py = mapPip(fx, fy)
    if kind == "large" then
      if rank == "A" then
        fillSuit(img, suit, px, py, 150, ink, false)
      else
        strokeCircle(img, px, py, 86, 4, GOLD)
        fillSuit(img, suit, px, py - 6, 118, ink, false)
        local tw = textWidth(rank, 3)
        stampText(img, rank, px - math.floor(tw / 2), py + 58, ink, 3, false)
      end
    else
      fillSuit(img, suit, px, py, 40, ink, flip)
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

local function drawCrescentMoon(img, body, shade, clipR)
  local mx, my, mr = 168, 274, 78
  local sx, sy, sr = 202, 254, 70
  local mrsq, srsq = mr * mr, sr * sr
  for y = my - mr, my + mr do
    for x = mx - mr, mx + mr do
      local dx, dy = x - mx, y - my
      if dx * dx + dy * dy <= mrsq then
        local dx2, dy2 = x - sx, y - sy
        if dx2 * dx2 + dy2 * dy2 > srsq then
          local t = (x - (mx - mr)) / (mr * 2)
          putInCircle(img, x, y, t < 0.38 and shade or body, clipR)
        end
      end
    end
  end
end

local function drawMoonFace(img, eyeWhite, pupil, mouth, clipR)
  fillEllipse(img, 154, 248, 8, 10, eyeWhite)
  fillEllipse(img, 150, 278, 7, 9, eyeWhite)
  fillCircleClipped(img, 156, 250, 4, pupil, clipR)
  fillCircleClipped(img, 152, 280, 3, pupil, clipR)
  putInCircle(img, 158, 247, WHITE, clipR)
  putInCircle(img, 154, 277, WHITE, clipR)
  for i = 0, 22 do
    local t = i / 22
    local x = 148 + math.floor(t * 28)
    local y = 302 + math.floor(math.sin(t * math.pi) * 8)
    putInCircle(img, x, y, mouth, clipR)
    putInCircle(img, x, y + 1, mouth, clipR)
    putInCircle(img, x, y + 2, mouth, clipR)
  end
end

local function drawBlood(img, clipR)
  for i = 0, 52 do
    local x = 172 + math.floor(i * 0.18)
    local y = 308 + i
    local r = (i < 8) and 3 or (i < 28 and 4 or 3)
    fillCircleClipped(img, x, y, r, i % 5 == 0 and BLOOD_DARK or BLOOD, clipR)
  end
  fillCircleClipped(img, 176, 322, 6, BLOOD, clipR)
  fillCircleClipped(img, 180, 340, 5, BLOOD, clipR)
  fillCircleClipped(img, 183, 356, 4, BLOOD, clipR)
  fillCircleClipped(img, 186, 370, 3, BLOOD, clipR)
  fillEllipse(img, 170, 310, 7, 4, BLOOD)
end

local function drawJokerCircleChrome(img, fill, ring)
  fillCircle(img, CX, CY, 120, fill)
  strokeCircle(img, CX, CY, 120, 5, ring)
  strokeCircle(img, CX, CY, 114, 2, GOLD_DIM)
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
    drawJokerCircleChrome(img, C(8, 16, 42), C(120, 170, 255))
    local seed = 17
    for i = 1, 56 do
      seed = (seed * 1103515245 + 12345) % 2147483647
      local ang = (seed % 360) * math.pi / 180
      seed = (seed * 1103515245 + 12345) % 2147483647
      local dist = 12 + (seed % 96)
      local x = CX + math.floor(math.cos(ang) * dist)
      local y = CY + math.floor(math.sin(ang) * dist)
      if inMainCircle(x, y, 108) then
        seed = (seed * 1103515245 + 12345) % 2147483647
        local kindStar = seed % 5
        local col = (kindStar == 0) and C(255, 232, 160) or (kindStar == 1 and C(200, 220, 255) or C(240, 246, 255))
        if kindStar >= 3 then
          fillStar(img, x, y, 5 + (seed % 4), 2, col)
        else
          fillSparkle(img, x, y, 2 + (seed % 4), col)
        end
      end
    end
    fillSparkle(img, CX - 36, CY - 28, 6, C(255, 248, 220))
    fillSparkle(img, CX + 40, CY + 18, 5, WHITE)
    fillStar(img, CX + 8, CY - 44, 8, 3, C(210, 230, 255))
    fillStar(img, CX - 20, CY + 40, 7, 3, C(255, 236, 170))
  end
end

local function drawCaption(img, text, col)
  local scale = 3
  local tw = textWidth(text, scale)
  stampText(img, text, math.floor((W - tw) / 2), H - 52, col, scale, false)
end

local function drawSpearIcon(img, col)
  fillDiamond(img, CX, 168, 16, 28, col)
  fillRect(img, CX - 5, 188, 11, 200, col)
  fillRect(img, CX - 28, 200, 56, 10, col)
  fillRect(img, CX - 18, 392, 36, 8, col)
end

local function drawPassIcon(img, col)
  strokeCircle(img, CX, CY - 10, 70, 6, col)
  fillPoly(img, { { CX - 10, CY - 50 }, { CX + 40, CY - 10 }, { CX - 10, CY + 30 } }, col)
  fillPoly(img, { { CX - 40, CY - 36 }, { CX - 4, CY - 10 }, { CX - 40, CY + 16 } }, col)
end

local function drawRevIcon(img, col)
  strokeCircle(img, CX, CY - 8, 62, 7, col)
  fillPoly(img, { { CX + 48, CY - 40 }, { CX + 78, CY - 8 }, { CX + 42, CY - 2 } }, col)
  fillPoly(img, { { CX - 48, CY + 24 }, { CX - 78, CY - 8 }, { CX - 42, CY - 14 } }, col)
end

local function drawCounterIcon(img, col)
  for i = -6, 6 do
    for t = 0, 150 do
      put(img, CX - 70 + t + i, CY - 70 + t, col)
      put(img, CX + 70 - t + i, CY - 70 + t, col)
    end
  end
end

local function drawMirrorIcon(img, col)
  fillRect(img, CX - 54, CY - 80, 108, 150, col)
  fillRect(img, CX - 44, CY - 70, 88, 130, FACE)
  fillRect(img, CX - 30, CY - 58, 18, 106, C(200, 214, 230))
  fillRect(img, CX + 16, CY - 40, 10, 50, C(180, 196, 220))
end

local function drawPillIcon(img, col)
  fillEllipse(img, CX - 40, CY - 8, 36, 28, col)
  fillRect(img, CX - 40, CY - 36, 80, 56, col)
  fillEllipse(img, CX + 40, CY - 8, 36, 28, FACE)
  fillRect(img, CX, CY - 36, 40, 56, FACE)
  fillEllipse(img, CX + 40, CY - 8, 36, 28, C(250, 248, 240))
  strokeCircle(img, CX + 40, CY - 8, 36, 3, col)
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
    drawSpearIcon(img, INK_BLACK)
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
    drawCounterIcon(img, INK_BLACK)
    drawCaption(img, "CTR", INK_BLACK)
  elseif id == "SPEC:MIRROR" then
    drawFaceBase(img, SPEC_BORDER)
    drawMirrorIcon(img, INK_BLUE)
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
  for y = 28, H - 29, 14 do
    for x = 28, W - 29, 14 do
      local on = ((x + y) % 28) < 14
      fillDiamond(img, x + 6, y + 6, 4, 5, on and NAVY_LIGHT or NAVY_MID)
    end
  end
  strokeRoundRect(img, 4, 6, GOLD)
  strokeRoundRect(img, 14, 3, NAVY_LIGHT)
  fillCircle(img, CX, CY, 78, NAVY)
  strokeCircle(img, CX, CY, 78, 5, GOLD)
  strokeCircle(img, CX, CY, 70, 2, GOLD_DIM)
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
