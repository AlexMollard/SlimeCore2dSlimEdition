#pragma once

#include <string>
#include <vector>

struct DialogueChoice
{
	std::string text;
	int nextNode = -1;
};

struct DialogueNode
{
	std::string line;
	std::vector<DialogueChoice> choices;
};

struct DialogueTree
{
	std::vector<DialogueNode> nodes;
};

// Super small dialogue database. Hardcode trees for now.
class DialogueDB
{
public:
	int Add(DialogueTree tree);
	const DialogueTree* Get(int id) const;

private:
	std::vector<DialogueTree> m_trees;
};
