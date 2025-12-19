#include "Snake.h"

#include <algorithm>
#include <iostream>
#include <random>

#include "Resources/ResourceManager.h"

namespace
{
	std::mt19937& Rng()
	{
		static std::mt19937 rng{ std::random_device{}() };
		return rng;
	}

	int RandInt(int minInclusive, int maxInclusive)
	{
		std::uniform_int_distribution<int> dist(minInclusive, maxInclusive);
		return dist(Rng());
	}

	glm::ivec2 RandDir4()
	{
		switch (RandInt(0, 3))
		{
		case 0: return { 1, 0 };
		case 1: return { -1, 0 };
		case 2: return { 0, 1 };
		default: return { 0, -1 };
		}
	}

	bool IsReverse(const glm::ivec2& a, const glm::ivec2& b)
	{
		return (a.x == -b.x && a.y == -b.y);
	}
} // namespace

Snake::Snake(Camera* cam, Renderer2D* rend, ObjectManager* objMan)
{
	m_camera = cam;
	m_renderer = rend;
	m_objManager = objMan;
	Input::GetInstance()->SetCamera(m_camera);

	// High scores (kept)
	std::string line;
	m_hsFile.open("..\\Txt\\HS.txt", std::ios::in);
	if (m_hsFile.is_open())
	{
		int index = 0;
		while (std::getline(m_hsFile, line))
		{
			if (line.empty()) continue;
			if (index >= (int)m_highScores.size()) break;
			m_highScores[(size_t)index] = std::stoi(line);
			++index;
		}
		m_hsFile.close();
	}

	// Text rendering
	m_textShader = ResourceManager::GetInstance().GetShader("text");
	if (!m_textShader)
		m_textShader = new Shader("Text Shader", "textVert.shader", "textFrag.shader");
	m_text = new Text();

	BuildViewportVisuals();
	GenerateWorld();
}

Snake::~Snake()
{
	// Save high scores
	m_hsFile.open("..\\Txt\\HS.txt", std::ios::out | std::ios::trunc);
	for (size_t i = 0; i < m_highScores.size(); i++)
		m_hsFile << std::to_string(m_highScores[i]) << std::endl;
	m_hsFile.close();

	delete m_world;
	m_world = nullptr;

	delete m_textShader;
	m_textShader = nullptr;
	delete m_text;
	m_text = nullptr;
}

void Snake::BuildViewportVisuals()
{
	// One-time creation of visible quads. We repaint colors every frame.
	for (int x = 0; x < VIEW_W; ++x)
	{
		for (int y = 0; y < VIEW_H; ++y)
		{
			const float worldX = (x - (VIEW_W * 0.5f)) * m_cellSpacing;
			const float worldY = (y - (VIEW_H * 0.5f)) * m_cellSpacing;
			m_view[x][y].visual = m_objManager->CreateQuad(glm::vec3(worldX, worldY, 0.0f), glm::vec2(m_cellSize), m_colGrass);
		}
	}
}

void Snake::GenerateWorld()
{
	// Bigger than the viewport, still small enough to be simple.
	const int W = 120;
	const int H = 120;
	m_world = new World(W, H);

	// Terrain base
	for (int y = 0; y < H; ++y)
	{
		for (int x = 0; x < W; ++x)
		{
			Tile& t = m_world->At({ x, y });
			t.terrain = Terrain::Grass;
			t.feature = Feature::None;
			t.item = ItemType::None;
			t.blocksMovement = false;
		}
	}

	// Stamp a few forests (trees block)
	for (int k = 0; k < 6; ++k)
	{
		const glm::ivec2 c = { RandInt(10, W - 11), RandInt(10, H - 11) };
		const int r = RandInt(4, 10);
		for (int dy = -r; dy <= r; ++dy)
		{
			for (int dx = -r; dx <= r; ++dx)
			{
				glm::ivec2 p = { c.x + dx, c.y + dy };
				if (!m_world->InBounds(p)) continue;
				if (dx*dx + dy*dy > r*r) continue;
				if (RandInt(0, 100) < 65)
				{
					Tile& t = m_world->At(p);
					t.feature = Feature::Tree;
					t.blocksMovement = true;
				}
			}
		}
	}

	// Stamp ore veins (ore blocks)
	for (int k = 0; k < 4; ++k)
	{
		const glm::ivec2 c = { RandInt(10, W - 11), RandInt(10, H - 11) };
		const int r = RandInt(3, 7);
		for (int dy = -r; dy <= r; ++dy)
		{
			for (int dx = -r; dx <= r; ++dx)
			{
				glm::ivec2 p = { c.x + dx, c.y + dy };
				if (!m_world->InBounds(p)) continue;
				if (dx*dx + dy*dy > r*r) continue;
				if (RandInt(0, 100) < 50)
				{
					Tile& t = m_world->At(p);
					t.feature = Feature::Ore;
					t.blocksMovement = true;
				}
			}
		}
	}

	// Scatter food items
	for (int i = 0; i < 80; ++i)
	{
		glm::ivec2 p = { RandInt(0, W - 1), RandInt(0, H - 1) };
		if (m_world->IsBlocked(p)) { --i; continue; }
		m_world->At(p).item = ItemType::Food;
	}

	// Create a tiny dialogue DB
	DialogueTree tree;
	{
		DialogueNode n0;
		n0.line = "Sss... hey traveller. The forest whispers, but the rocks shout.";
		n0.choices = { {"How do I get wood?", 1}, {"Any advice?", 2}, {"Bye", -1} };
		DialogueNode n1;
		n1.line = "Trees are stubborn. Stand next to one and press E to chop. You'll get wood.";
		n1.choices = { {"Got it", -1} };
		DialogueNode n2;
		n2.line = "Don't bite monsters. They bite back. If you must, be longer than them.";
		n2.choices = { {"Wise", -1} };
		tree.nodes = { n0, n1, n2 };
	}
	const int npcDialogueId = m_dialogues.Add(tree);

	// Spawn player in the middle-ish on a free tile
	glm::ivec2 spawn = { W / 2, H / 2 };
	while (m_world->IsBlocked(spawn)) spawn.x++;

	m_player.isPlayer = true;
	m_player.dir = { 0, 0 };
	m_player.lastDir = { 0, 0 };
	m_player.pendingGrow = 2;
	{
		EntityId head = m_world->CreateEntity(EntityType::PlayerSnake, spawn, true, "You");
		m_player.headEntity = head;
		m_player.body.clear();
		m_player.body.push_front(spawn);
		// Initial length
		m_player.body.push_back(spawn);
		m_player.body.push_back(spawn);
	}

	// Spawn one NPC snake
	{
		SnakeAgent npc;
		npc.isPlayer = false;
		npc.dir = RandDir4();
		npc.lastDir = npc.dir;
		glm::ivec2 p = spawn + glm::ivec2(8, 4);
		while (m_world->IsBlocked(p)) p += glm::ivec2(1, 0);
		EntityId head = m_world->CreateEntity(EntityType::NpcSnake, p, true, "Sage Snake");
		Entity* e = m_world->GetEntity(head);
		if (e) e->dialogueId = npcDialogueId;
		npc.headEntity = head;
		npc.body = { p, p, p };
		m_npcs.push_back(npc);
	}

	// Spawn a monster
	{
		MonsterAgent m;
		glm::ivec2 p = spawn + glm::ivec2(-12, -6);
		while (m_world->IsBlocked(p)) p += glm::ivec2(1, 0);
		m.entity = m_world->CreateEntity(EntityType::Monster, p, true, "Chomper");
		m.dir = RandDir4();
		m_monsters.push_back(m);
	}

	PaintViewport();
}

glm::ivec2 Snake::PlayerPos() const
{
	if (m_player.body.empty()) return { 0, 0 };
	return m_player.body.front();
}

glm::ivec2 Snake::ClampToWorld(const glm::ivec2& p) const
{
	glm::ivec2 out = p;
	out.x = glm::clamp(out.x, 0, m_world->Width() - 1);
	out.y = glm::clamp(out.y, 0, m_world->Height() - 1);
	return out;
}

glm::ivec2 Snake::WorldToViewOrigin() const
{
	// Center player in view.
	glm::ivec2 center = PlayerPos();
	glm::ivec2 origin = { center.x - VIEW_W / 2, center.y - VIEW_H / 2 };
	// clamp origin so viewport stays within world bounds
	origin.x = glm::clamp(origin.x, 0, m_world->Width() - VIEW_W);
	origin.y = glm::clamp(origin.y, 0, m_world->Height() - VIEW_H);
	return origin;
}

void Snake::UpdateCameraFollow()
{
	// Your camera currently... is orthographic and used by Renderer2D.
	// We keep camera at (0,0) because we render quads as a fixed screen grid.
	// If you later want camera to pan in world space, move the quads instead.
}

void Snake::PaintViewport()
{
	if (!m_world) return;

	const glm::ivec2 origin = WorldToViewOrigin();

	// Build quick lookup of snake bodies for drawing.
	auto paintSnake = [&](const SnakeAgent& s, const glm::vec3& headC, const glm::vec3& bodyC)
	{
		for (size_t i = 0; i < s.body.size(); ++i)
		{
			const glm::ivec2 wp = s.body[i];
			if (wp.x < origin.x || wp.y < origin.y || wp.x >= origin.x + VIEW_W || wp.y >= origin.y + VIEW_H)
				continue;
			const int vx = wp.x - origin.x;
			const int vy = wp.y - origin.y;
			m_view[vx][vy].SetColor(i == 0 ? headC : bodyC);
		}
	};

	// 1) Terrain + features/items
	for (int vx = 0; vx < VIEW_W; ++vx)
	{
		for (int vy = 0; vy < VIEW_H; ++vy)
		{
			const glm::ivec2 wp = { origin.x + vx, origin.y + vy };
			const Tile& t = m_world->At(wp);

			glm::vec3 c;
			switch (t.terrain)
			{
			case Terrain::Grass: c = m_colGrass; break;
			case Terrain::Dirt:  c = m_colDirt; break;
			case Terrain::Water: c = m_colWater; break;
			case Terrain::Rock:  c = m_colRock; break;
			default: c = m_colGrass; break;
			}

			// Features override terrain
			if (t.feature == Feature::Tree) c = m_colTree;
			else if (t.feature == Feature::Ore) c = m_colOre;

			// Items (food) override lightly
			if (t.item == ItemType::Food) c = m_colFood;

			m_view[vx][vy].SetColor(c);
		}
	}

	// 2) Monsters (single-tile)
	for (const auto& m : m_monsters)
	{
		const Entity* e = m_world->GetEntity(m.entity);
		if (!e) continue;
		const glm::ivec2 wp = e->pos;
		if (wp.x < origin.x || wp.y < origin.y || wp.x >= origin.x + VIEW_W || wp.y >= origin.y + VIEW_H)
			continue;
		m_view[wp.x - origin.x][wp.y - origin.y].SetColor(m_colMonster);
	}

	// 3) Snakes
	paintSnake(m_player, m_colPlayer * glm::vec3(1.2f), m_colPlayer);
	for (const auto& npc : m_npcs)
		paintSnake(npc, m_colNpc * glm::vec3(1.2f), m_colNpc);
}

void Snake::HandleInput()
{
	if (m_inDialogue)
	{
		// Z/X/C pick dialogue options (kept to simple letter keys)
		const Entity* npc = m_world->GetEntity(m_talkingTo);
		if (!npc) { m_inDialogue = false; return; }
		const DialogueTree* tree = m_dialogues.Get(npc->dialogueId);
		if (!tree) { m_inDialogue = false; return; }
		if (m_dialogueNode < 0 || m_dialogueNode >= (int)tree->nodes.size()) { m_inDialogue = false; return; }
		const DialogueNode& node = tree->nodes[(size_t)m_dialogueNode];

		int picked = -1;
		if (Input::GetKeyRelease(Keycode::Z)) picked = 0;
		if (Input::GetKeyRelease(Keycode::X)) picked = 1;
		if (Input::GetKeyRelease(Keycode::C)) picked = 2;
		if (picked >= 0 && picked < (int)node.choices.size())
		{
			m_dialogueNode = node.choices[(size_t)picked].nextNode;
			if (m_dialogueNode < 0) m_inDialogue = false;
		}
		if (Input::GetKeyRelease(Keycode::Q))
			m_inDialogue = false;

		return;
	}

	// Movement input (grid)
	const bool left = Input::GetKeyPress(Keycode::LEFT) || Input::GetKeyPress(Keycode::A);
	const bool up = Input::GetKeyPress(Keycode::UP) || Input::GetKeyPress(Keycode::W);
	const bool right = Input::GetKeyPress(Keycode::RIGHT) || Input::GetKeyPress(Keycode::D);
	const bool down = Input::GetKeyPress(Keycode::DOWN) || Input::GetKeyPress(Keycode::S);

	glm::ivec2 newDir = m_player.dir;
	if (left) newDir = { -1, 0 };
	else if (up) newDir = { 0, 1 };
	else if (right) newDir = { 1, 0 };
	else if (down) newDir = { 0, -1 };

	TryMoveSnake(m_player, newDir);

	// Interaction
	if (Input::GetKeyRelease(Keycode::E))
		TryInteract();
}

bool Snake::TryMoveSnake(SnakeAgent& s, const glm::ivec2& newDir)
{
	if (newDir == glm::ivec2(0,0)) return false;
	if (s.lastDir != glm::ivec2(0,0) && IsReverse(newDir, s.lastDir))
		return false;
	s.dir = newDir;
	return true;
}

bool Snake::StepSnake(SnakeAgent& s, bool allowWrap)
{
	if (!s.alive || s.dir == glm::ivec2(0,0) || s.body.empty()) return false;

	const glm::ivec2 head = s.body.front();
	glm::ivec2 next = head + s.dir;

	if (allowWrap)
	{
		if (next.x < 0) next.x = m_world->Width() - 1;
		if (next.y < 0) next.y = m_world->Height() - 1;
		if (next.x >= m_world->Width()) next.x = 0;
		if (next.y >= m_world->Height()) next.y = 0;
	}

	if (!m_world->InBounds(next))
		return false;

	// Collision with terrain/features/entities
	// Allow stepping onto your own tail if it will move away this tick (no grow)
	const glm::ivec2 oldTail = s.body.back();
	const bool willGrow = (s.pendingGrow > 0);
	if (m_world->IsBlocked(next))
	{
		// Special case: your tail cell is occupied by your own head-entity in occupancy, so we can't rely on occupancy.
		if (!( !willGrow && next == oldTail ))
			return false;
	}

	// Self collision check against body (excluding tail if it moves)
	for (size_t i = 0; i < s.body.size(); ++i)
	{
		if (!willGrow && i == s.body.size() - 1 && next == oldTail)
			continue;
		if (s.body[i] == next)
			return false;
	}

	// Items
	Tile& tile = m_world->At(next);
	if (tile.item == ItemType::Food)
	{
		tile.item = ItemType::None;
		s.pendingGrow += 1;
		if (s.isPlayer) ++m_score;
	}

	// Move occupancy for head entity (snake occupies via head entity for simplicity)
	Entity* headEntity = m_world->GetEntity(s.headEntity);
	if (headEntity)
	{
		m_world->SetOccupant(head, INVALID_ENTITY);
		headEntity->pos = next;
		m_world->SetOccupant(next, s.headEntity);
	}

	// Update body
	s.body.push_front(next);
	if (s.pendingGrow > 0)
	{
		--s.pendingGrow;
	}
	else
	{
		s.body.pop_back();
	}

	s.lastDir = s.dir;
	return true;
}

void Snake::StepNpc(SnakeAgent& s)
{
	// Very simple: occasionally change dir, avoid reversal, avoid blocked.
	if (RandInt(0, 100) < 20)
	{
		glm::ivec2 d = RandDir4();
		if (!IsReverse(d, s.lastDir)) s.dir = d;
	}

	// Try forward; if blocked, try a few other dirs
	for (int tries = 0; tries < 4; ++tries)
	{
		if (StepSnake(s, false)) return;
		s.dir = RandDir4();
		if (IsReverse(s.dir, s.lastDir)) s.dir = s.lastDir; // shrug
	}
}

void Snake::StepMonster(MonsterAgent& m)
{
	Entity* e = m_world->GetEntity(m.entity);
	if (!e) return;

	m.thinkTimer -= m_tick;
	if (m.thinkTimer <= 0.0f)
	{
		m.thinkTimer = 0.45f + (RandInt(0, 100) / 200.0f);
		const glm::ivec2 ppos = PlayerPos();
		const glm::ivec2 d = ppos - e->pos;
		const int manhattan = std::abs(d.x) + std::abs(d.y);
		if (manhattan <= 10)
		{
			// Chase: step along bigger axis
			if (std::abs(d.x) > std::abs(d.y)) m.dir = { (d.x > 0) ? 1 : -1, 0 };
			else m.dir = { 0, (d.y > 0) ? 1 : -1 };
		}
		else
		{
			m.dir = RandDir4();
		}
	}

	glm::ivec2 next = e->pos + m.dir;
	if (!m_world->InBounds(next) || m_world->IsBlocked(next))
	{
		m.dir = RandDir4();
		return;
	}

	// Move monster occupancy
	m_world->SetOccupant(e->pos, INVALID_ENTITY);
	e->pos = next;
	m_world->SetOccupant(e->pos, m.entity);

	// If it hits player head, reset player (very simple 'death')
	if (next == PlayerPos())
	{
		// Lose score chunk and shrink a bit
		m_score = std::max(0, m_score - 3);
		if (m_player.body.size() > 3) m_player.body.resize(3);
		m_player.pendingGrow = 2;
	}
}

void Snake::TryInteract()
{
	const glm::ivec2 p = PlayerPos();
	// Check 4-neighborhood for NPC or feature
	const glm::ivec2 dirs[4] = { {1,0},{-1,0},{0,1},{0,-1} };
	for (const auto& d : dirs)
	{
		const glm::ivec2 q = p + d;
		if (!m_world->InBounds(q)) continue;

		// NPC talk
		EntityId occ = m_world->Occupant(q);
		if (occ != INVALID_ENTITY)
		{
			const Entity* e = m_world->GetEntity(occ);
			if (e && e->type == EntityType::NpcSnake && e->dialogueId >= 0)
			{
				m_inDialogue = true;
				m_talkingTo = occ;
				m_dialogueNode = 0;
				return;
			}
		}

		// Chop tree / mine ore
		Tile& t = m_world->At(q);
		if (t.feature == Feature::Tree)
		{
			t.feature = Feature::None;
			t.blocksMovement = false;
			// Drop wood as item on that tile (if free)
			t.item = ItemType::Wood;
			return;
		}
		if (t.feature == Feature::Ore)
		{
			t.feature = Feature::None;
			t.blocksMovement = false;
			t.item = ItemType::OreChunk;
			return;
		}

		// Pick up item
		if (t.item != ItemType::None)
		{
			if (t.item == ItemType::Food)
			{
				m_player.pendingGrow += 1;
				++m_score;
			}
			// Other items could go to inventory later
			t.item = ItemType::None;
			return;
		}
	}
}

void Snake::TickSimulation()
{
	// Player moves first
	const bool moved = StepSnake(m_player, false);
	if (!moved && m_player.dir != glm::ivec2(0,0))
	{
		// Hit a wall/object/self: simple penalty and stop moving
		m_player.dir = {0,0};
		m_player.lastDir = {0,0};
		m_score = std::max(0, m_score - 1);
	}

	for (auto& npc : m_npcs)
		StepNpc(npc);
	for (auto& mon : m_monsters)
		StepMonster(mon);
}

void Snake::Update(float deltaTime)
{
	if (!m_world) return;

	m_timer += deltaTime;

	HandleInput();

	// Dialogue mode pauses simulation ticks
	if (m_inDialogue)
	{
		PaintViewport();
		// Render dialogue UI
		const Entity* npc = m_world->GetEntity(m_talkingTo);
		const DialogueTree* tree = npc ? m_dialogues.Get(npc->dialogueId) : nullptr;
		if (npc && tree && m_dialogueNode >= 0 && m_dialogueNode < (int)tree->nodes.size())
		{
			const DialogueNode& node = tree->nodes[(size_t)m_dialogueNode];
			m_text->RenderText(*m_textShader, npc->name + ":", 30.0f, 170.0f, 0.9f, glm::vec3(1.0f));
			m_text->RenderText(*m_textShader, node.line, 30.0f, 140.0f, 0.8f, glm::vec3(0.9f));
			static const char* keys[3] = { "Z", "X", "C" };
			for (size_t i = 0; i < node.choices.size() && i < 3; ++i)
			{
				std::string opt = std::string(keys[i]) + ") " + node.choices[i].text;
				m_text->RenderText(*m_textShader, opt, 30.0f, 110.0f - (float)i * 25.0f, 0.8f, glm::vec3(0.8f));
			}
			m_text->RenderText(*m_textShader, "(Z/X/C choose, ESC exit)", 30.0f, 40.0f, 0.7f, glm::vec3(0.7f));
		}
		return;
	}

	// Tick-based sim
	const float dynamicTick = std::max(m_minTick, (m_tick - (float)m_player.body.size() * 0.002f));
	if (m_timer >= dynamicTick)
	{
		TickSimulation();
		m_timer = 0.0f;
	}

	PaintViewport();

	// HUD
	m_text->RenderText(*m_textShader, "Open-World Snake", 25.0f, 25.0f, 1.0f, glm::vec3(1.0f));
	m_text->RenderText(*m_textShader, "Score: " + std::to_string(m_score), 25.0f, 55.0f, 0.8f, glm::vec3(0.7f, 1.0f, 0.7f));
	m_text->RenderText(*m_textShader, "E: interact   WASD/Arrows: move", 25.0f, 80.0f, 0.7f, glm::vec3(0.8f));

	for (size_t i = 0; i < m_highScores.size(); i++)
	{
		std::string highScore = "HS" + std::to_string((int)i + 1) + ": " + std::to_string(m_highScores[i]);
		m_text->RenderText(*m_textShader, highScore, 25.0f, 900.0f - (50.0f * (float)i), 0.8f, glm::vec3(0.0f, 1.0f, 0.0f));
	}
}
