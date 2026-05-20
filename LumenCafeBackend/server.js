import express from "express";
import cors from "cors";
import dotenv from "dotenv";
import fs from "fs";
import path from "path";
import OpenAI from "openai";
import { fileURLToPath } from "url";

dotenv.config();

const app = express();
app.use(cors());
app.use(express.json({ limit: "1mb" }));

const openai = new OpenAI({
  apiKey: process.env.OPENAI_API_KEY,
});

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

function loadPromptTemplate() {
  const promptPath = path.join(__dirname, "prompts", "cafe_prompt.md");
  return fs.readFileSync(promptPath, "utf8");
}

function buildAssignmentsText(assignments) {
  if (!assignments || assignments.length === 0) {
    return "No roles were assigned.";
  }

  return assignments
    .map((a) => {
      if (!a.subject) {
        return `Role: ${a.panelName}\nAssigned Subject: None`;
      }

      return [
        `Role: ${a.panelName}`,
        `Assigned Subject Type: ${a.subject.type}`,
        `ID: ${a.subject.id}`,
        `Description: ${a.subject.description}`,
        a.subject.traitRatings
          ? [
              "Traits:",
              `- Curiosity: ${a.subject.traitRatings.curiosity}`,
              `- Discipline: ${a.subject.traitRatings.discipline}`,
              `- Drive: ${a.subject.traitRatings.drive}`,
              `- Empathy: ${a.subject.traitRatings.empathy}`,
              `- Instability: ${a.subject.traitRatings.instability}`,
              `- Sincerity: ${a.subject.traitRatings.sincerity}`,
            ].join("\n")
          : "",
      ]
        .filter(Boolean)
        .join("\n");
    })
    .join("\n\n");
}

app.post("/api/simulate", async (req, res) => {
  try {
    const { assignments } = req.body;

    const template = loadPromptTemplate();
    const assignmentsText = buildAssignmentsText(assignments);
    const finalPrompt = template.replace("{{ASSIGNMENTS}}", assignmentsText);

    const response = await openai.responses.create({
      model: "gpt-5.4-mini",
      input: finalPrompt,
    });

    res.json({
      text: response.output_text,
    });
  } catch (error) {
    console.error(error);
    res.status(500).json({
      error: "Failed to simulate cafe ending.",
    });
  }
});

const port = process.env.PORT || 3000;
app.listen(port, () => {
  console.log(`Lumen Cafe backend running on http://localhost:${port}`);
});