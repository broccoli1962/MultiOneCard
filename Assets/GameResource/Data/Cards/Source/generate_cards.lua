-- OneTable Official 89 CardDefId fronts + BACK. Suit template + rank stamps.
-- Star (R) and Moon (M) use different silhouettes.

local OUT = [[D:/Unity/MultiOneCard/Assets/GameResource/Data/Cards]]
local SRC = OUT .. "/Source/OneTableCards.aseprite"
local W, H = 64, 90

local function C(r, g, b, a)
  return Color{ r = r, g = g, b = b, a = a or 255 }
end

local FACE = C(248, 240, 220)
local FACE_INNER = C(236, 224, 196)
local INK_BLACK = C(28, 28, 32)
local INK_RED = C(196, 42, 48)
local INK_BLUE = C(36, 78, 186)
local GOLD = C(196, 164, 72)
local NAVY = C(24, 32, 58)
local NAVY_DARK = C(14, 18, 36)
local NAVY_LIGHT = C(42, 56, 96)
local CREAM = C(255, 250, 236)
local SPEC_BORDER = C(92, 72, 36)
local WHITE = C(255, 255, 255)
local GRAY = C(90, 90, 96)
local PILL_BK = C(36, 36, 40)
local PILL_RD = C(200, 48, 54)
local PILL_BL = C(40, 86, 196)
local JOKER_C1 = C(220, 64, 72)
local JOKER_C2 = C(48, 96, 210)
local JOKER_C3 = C(236, 196, 48)
local JOKER_MOON = C(160, 190, 255)

local GLYPH = {
  ["0"] = { "###", "#.#", "#.#", "#.#", "###" },
  ["1"] = { ".#.", "##.", ".#.", ".#.", "###" },
  ["2"] = { "###", "..#", "###", "#..", "###" },
  ["3"] = { "###", "..#", "###", "..#", "###" },
  ["4"] = { "#.#", "#.#", "###", "..#", "..#" },
  ["5"] = { "###", "#..", "###", "..#", "###" },
  ["6"] = { "###", "#..", "###", "#.#", "###" },
  ["7"] = { "###", "..#", ".#.", ".#.", ".#." },
  ["8"] = { "###", "#.#", "###", "#.#", "###" },
  ["9"] = { "###", "#.#", "###", "..#", "###" },
  ["A"] = { ".#.", "#.#", "###", "#.#", "#.#" },
  ["J"] = { "###", ".#.", ".#.", "#.#", ".##" },
  ["Q"] = { "###", "#.#", "#.#", "##.", ".##" },
  ["K"] = { "#.#", "#.#", "##.", "#.#", "#.#" },
  ["B"] = { "##.", "#.#", "##.", "#.#", "##." },
  ["C"] = { "###", "#..", "#..", "#..", "###" },
  ["D"] = { "##.", "#.#", "#.#", "#.#", "##." },
  ["E"] = { "###", "#..", "###", "#..", "###" },
  ["G"] = { "###", "#..", "#.#", "#.#", "###" },
  ["H"] = { "#.#", "#.#", "###", "#.#", "#.#" },
  ["I"] = { "###", ".#.", ".#.", ".#.", "###" },
  ["L"] = { "#..", "#..", "#..", "#..", "###" },
  ["M"] = { "#.#", "###", "#.#", "#.#", "#.#" },
  ["N"] = { "#.#", "###", "###", "#.#", "#.#" },
  ["O"] = { "###", "#.#", "#.#", "#.#", "###" },
  ["P"] = { "###", "#.#", "###", "#..", "#.." },
  ["R"] = { "###", "#.#", "##.", "#.#", "#.#" },
  ["S"] = { "###", "#..", "###", "..#", "###" },
  ["T"] = { "###", ".#.", ".#.", ".#.", ".#." },
  ["U"] = { "#.#", "#.#", "#.#", "#.#", "###" },
  ["V"] = { "#.#", "#.#", "#.#", "#.#", ".#." },
  ["W"] = { "#.#", "#.#", "#.#", "###", "#.#" },
  ["X"] = { "#.#", "#.#", ".#.", "#.#", "#.#" },
  ["Y"] = { "#.#", "#.#", ".#.", ".#.", ".#." },
  ["+"] = { "...", ".#.", "###", ".#.", "..." },
}

-- 9x9 suit silhouettes. Star vs Moon must not share the same outline.
local SUIT = {
  S = { -- spade
    "...#...",
    "..###..",
    ".#####.",
    "#######",
    "#######",
    ".#####.",
    "...#...",
    "..###..",
    "...#...",
  },
  H = { -- heart
    ".#...#.",
    "###.###",
    "#######",
    "#######",
    ".#####.",
    "..###..",
    "...#...",
    ".......",
    ".......",
  },
  D = { -- diamond
    "...#...",
    "..###..",
    ".#####.",
    "#######",
    ".#####.",
    "..###..",
    "...#...",
    ".......",
    ".......",
  },
  C = { -- club
    "..###..",
    ".#####.",
    "..###..",
    "#.###.#",
    "#######",
    "#.###.#",
    "...#...",
    "..###..",
    "...#...",
  },
  R = { -- star (pointed)
    "...#...",
    "...#...",
    "#..#..#",
    ".#####.",
    "..###..",
    "#######",
    ".#####.",
    "#..#..#",
    "...#...",
  },
  M = { -- moon crescent
    "..####.",
    ".#####.",
    "####...",
    "###....",
    "###....",
    "###....",
    "####...",
    ".#####.",
    "..####.",
  },
}

local ICON = {
  SPEAR = {
    ".......#.......",
    "......###......",
    ".......#.......",
    ".......#.......",
    ".......#.......",
    ".......#.......",
    ".......#.......",
    ".......#.......",
    ".......#.......",
    ".......#.......",
    "......###......",
    ".......#.......",
    "...............",
    "...............",
    "...............",
  },
  PASS = {
    "....###........",
    "...#...#.......",
    "..#.....#......",
    ".#.......#.....",
    "#.........#....",
    ".#.......#.....",
    "..#.....#......",
    "...#...#.......",
    "....###........",
    "...............",
    "....#####......",
    ".......#.......",
    "......#........",
    ".....#.........",
    "...............",
  },
  REV = {
    "..######.......",
    ".#......#......",
    "#........#.....",
    "#...##...#.....",
    "#...##...#.....",
    "#........#.....",
    ".#......#......",
    "..######.......",
    "...............",
    "......#........",
    ".....###.......",
    "....#####......",
    "......#........",
    "......#........",
    "...............",
  },
  CTR = {
    "#...........#..",
    ".#.........#...",
    "..#...#...#....",
    "...#.###.#.....",
    "....#####......",
    "...#.###.#.....",
    "..#...#...#....",
    ".#.........#...",
    "#...........#..",
    "...............",
    "...............",
    "...............",
    "...............",
    "...............",
    "...............",
  },
  MIRR = {
    "..#########....",
    ".#.........#...",
    "#..#######..#..",
    "#.#.......#.#..",
    "#.#.......#.#..",
    "#.#.......#.#..",
    "#.#.......#.#..",
    "#..#######..#..",
    ".#.........#...",
    "..#########....",
    "...............",
    "...............",
    "...............",
    "...............",
    "...............",
  },
  PILL = {
    "...............",
    "....#####......",
    "...#######.....",
    "..#########....",
    "..####.........",
    "..####.........",
    "..#########....",
    "...#######.....",
    "....#####......",
    "...............",
    "...............",
    "...............",
    "...............",
    "...............",
    "...............",
  },
  JOKER = {
    "...#.#.#.......",
    "..#######......",
    ".##.#.#.##.....",
    ".#########.....",
    "..##...##......",
    "...#####.......",
    "....###........",
    "...#####.......",
    "..##.#.##......",
    ".##.....##.....",
    "...............",
    "...............",
    "...............",
    "...............",
    "...............",
  },
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

local function stamp(img, rows, ox, oy, col, scale, flip)
  scale = scale or 1
  local hh = #rows
  local ww = #rows[1]
  for y = 1, hh do
    local row = rows[y]
    for x = 1, #row do
      if row:sub(x, x) == "#" then
        local px = x - 1
        local py = y - 1
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

local function stampText(img, text, ox, oy, col, scale)
  scale = scale or 1
  local x = ox
  for i = 1, #text do
    local ch = text:sub(i, i)
    local g = GLYPH[ch]
    if g then
      stamp(img, g, x, oy, col, scale, false)
      x = x + (3 * scale) + scale
    elseif ch == " " then
      x = x + 2 * scale
    end
  end
end

local function textWidth(text, scale)
  scale = scale or 1
  local w = 0
  for i = 1, #text do
    local ch = text:sub(i, i)
    if GLYPH[ch] then
      w = w + (3 * scale) + scale
    elseif ch == " " then
      w = w + 2 * scale
    end
  end
  if w > 0 then
    w = w - scale
  end
  return w
end

local function roundedFill(img, col)
  for y = 0, H - 1 do
    for x = 0, W - 1 do
      local corner = (x < 2 and y < 2) or (x > W - 3 and y < 2) or (x < 2 and y > H - 3) or (x > W - 3 and y > H - 3)
      if not corner then
        put(img, x, y, col)
      end
    end
  end
end

local function drawBorder(img, col)
  for x = 1, W - 2 do
    put(img, x, 1, col)
    put(img, x, H - 2, col)
  end
  for y = 1, H - 2 do
    put(img, 1, y, col)
    put(img, W - 2, y, col)
  end
  put(img, 2, 2, col)
  put(img, W - 3, 2, col)
  put(img, 2, H - 3, col)
  put(img, W - 3, H - 3, col)
end

local function drawInnerLine(img, col)
  for x = 3, W - 4 do
    put(img, x, 3, col)
    put(img, x, H - 4, col)
  end
  for y = 3, H - 4 do
    put(img, 3, y, col)
    put(img, W - 4, y, col)
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

local function drawRank(img, rank, ink)
  local scale = 2
  stampText(img, rank, 5, 5, ink, scale)
  local tw = textWidth(rank, scale)
  stampText(img, rank, W - 5 - tw, H - 5 - 5 * scale, ink, scale)
end

local function drawSmallSuit(img, suit, ink)
  stamp(img, SUIT[suit], 6, 18, ink, 1, false)
  stamp(img, SUIT[suit], W - 6 - 7, H - 18 - 9, ink, 1, true)
end

local function drawCenterSuit(img, suit, ink)
  local rows = SUIT[suit]
  local scale = 3
  local ww = #rows[1] * scale
  local hh = #rows * scale
  stamp(img, rows, math.floor((W - ww) / 2), math.floor((H - hh) / 2) + 2, ink, scale, false)
end

local function drawFaceBase(img, border)
  roundedFill(img, FACE)
  fillRect(img, 8, 10, W - 16, H - 20, FACE_INNER)
  drawBorder(img, border)
  drawInnerLine(img, GOLD)
end

local function drawTrump(img, suit, rank)
  local ink = suitInk(suit)
  drawFaceBase(img, ink)
  drawRank(img, rank, ink)
  drawSmallSuit(img, suit, ink)
  drawCenterSuit(img, suit, ink)
end

local function drawIconCenter(img, rows, col)
  local scale = 2
  local ww = #rows[1] * scale
  local hh = #rows * scale
  stamp(img, rows, math.floor((W - ww) / 2), math.floor((H - hh) / 2) - 4, col, scale, false)
end

local function drawCaption(img, text, col)
  local scale = 1
  local tw = textWidth(text, scale)
  stampText(img, text, math.floor((W - tw) / 2), H - 14, col, scale)
end

local function drawJoker(img, kind)
  if kind == "COLOR" then
    drawFaceBase(img, JOKER_C1)
    drawIconCenter(img, ICON.JOKER, JOKER_C2)
    stamp(img, ICON.JOKER, 8, 22, JOKER_C1, 1, false)
    stamp(img, ICON.JOKER, W - 23, 22, JOKER_C3, 1, false)
    drawCaption(img, "COLOR", JOKER_C1)
  elseif kind == "BW" then
    drawFaceBase(img, INK_BLACK)
    drawIconCenter(img, ICON.JOKER, GRAY)
    drawCaption(img, "BW", INK_BLACK)
  else
    drawFaceBase(img, INK_BLUE)
    drawIconCenter(img, ICON.JOKER, JOKER_MOON)
    stamp(img, SUIT.M, 8, 20, INK_BLUE, 1, false)
    stamp(img, SUIT.M, W - 17, 20, INK_BLUE, 1, false)
    drawCaption(img, "MOON", INK_BLUE)
  end
end

local function drawPill(img, col, caption)
  drawFaceBase(img, col)
  drawIconCenter(img, ICON.PILL, col)
  drawCaption(img, caption, col)
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
    drawIconCenter(img, ICON.SPEAR, INK_BLACK)
    drawCaption(img, "SPEAR", INK_BLACK)
  elseif id == "SPEC:PASS" then
    drawFaceBase(img, SPEC_BORDER)
    drawIconCenter(img, ICON.PASS, INK_BLACK)
    drawCaption(img, "PASS", INK_BLACK)
  elseif id == "SPEC:REVJOKER" then
    drawFaceBase(img, SPEC_BORDER)
    drawIconCenter(img, ICON.REV, INK_BLACK)
    drawCaption(img, "REV", INK_BLACK)
  elseif id == "SPEC:COUNTER" then
    drawFaceBase(img, SPEC_BORDER)
    drawIconCenter(img, ICON.CTR, INK_BLACK)
    drawCaption(img, "CTR", INK_BLACK)
  elseif id == "SPEC:MIRROR" then
    drawFaceBase(img, SPEC_BORDER)
    drawIconCenter(img, ICON.MIRR, INK_BLUE)
    drawCaption(img, "MIRROR", INK_BLUE)
  elseif id == "SPEC:PILL_BK" then
    drawPill(img, PILL_BK, "PILL BK")
  elseif id == "SPEC:PILL_RD" then
    drawPill(img, PILL_RD, "PILL RD")
  elseif id == "SPEC:PILL_BL" then
    drawPill(img, PILL_BL, "PILL BL")
  end
end

local function drawBack(img)
  roundedFill(img, NAVY_DARK)
  for y = 6, H - 7, 4 do
    for x = 6, W - 7, 4 do
      local on = ((x + y) % 8) < 4
      put(img, x, y, on and NAVY_LIGHT or NAVY)
      put(img, x + 1, y + 1, on and NAVY or NAVY_LIGHT)
    end
  end
  drawBorder(img, GOLD)
  drawInnerLine(img, NAVY_LIGHT)
  local tw = textWidth("OT", 2)
  stampText(img, "OT", math.floor((W - tw) / 2), math.floor(H / 2) - 6, GOLD, 2)
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
local suitLayer = spr:newLayer()
suitLayer.name = "Suit"
local rankLayer = spr:newLayer()
rankLayer.name = "Rank"

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
      drawFaceBase(img, suitInk(card.suit))
    end)
    paintCel(suitLayer, frame, function(img)
      local ink = suitInk(card.suit)
      drawSmallSuit(img, card.suit, ink)
      drawCenterSuit(img, card.suit, ink)
    end)
    paintCel(rankLayer, frame, function(img)
      drawRank(img, card.rank, suitInk(card.suit))
    end)
    drawTrump(composed, card.suit, card.rank)
  else
    paintCel(bgLayer, frame, function(img)
      drawSpecial(img, card.id)
    end)
    paintCel(suitLayer, frame, function(img) end)
    paintCel(rankLayer, frame, function(img) end)
    drawSpecial(composed, card.id)
  end
  composed:saveAs(OUT .. "/" .. fileName(card.id))
end

local backFrame = spr.frames[#cards + 1]
paintCel(bgLayer, backFrame, function(img)
  drawBack(img)
end)
paintCel(suitLayer, backFrame, function(img) end)
paintCel(rankLayer, backFrame, function(img) end)
local backImg = Image(W, H, ColorMode.RGB)
drawBack(backImg)
backImg:saveAs(OUT .. "/BACK.png")

spr:saveAs(SRC)
print("exported " .. #cards .. " fronts + BACK")
