#include "Dialogue.h"

int DialogueDB::Add(DialogueTree tree)
{
	m_trees.push_back(std::move(tree));
	return (int) m_trees.size() - 1;
}

const DialogueTree* DialogueDB::Get(int id) const
{
	if (id < 0 || id >= (int) m_trees.size())
		return nullptr;
	return &m_trees[(size_t) id];
}
