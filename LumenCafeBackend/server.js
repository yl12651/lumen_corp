import express from "express";
import cors from "cors";
import dotenv from "dotenv";
import fs from "fs";
import path from "path";
import OpenAI from "openai";
import { fileURLToPath } from "url";

dotenv.config();

const openaiModel = process.env.OPENAI_MODEL || "gpt-5.4-mini";
const openaiTimeoutMs = Number(process.env.OPENAI_TIMEOUT_MS || 180000);

const app = express();
app.use(cors());
app.use(express.json({ limit: "1mb" }));

const openai = new OpenAI({
  apiKey: process.env.OPENAI_API_KEY,
  timeout: openaiTimeoutMs,
  maxRetries: 2,
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

function formatSubject(subject) {
  if (!subject) {
    return "Assigned Subject: None";
  }

  return [
    `Assigned Subject Type: ${subject.type}`,
    `ID: ${subject.id}`,
    `Description: ${subject.description}`,
    subject.traitRatings
      ? [
          "Traits:",
          `- Curiosity: ${subject.traitRatings.curiosity}`,
          `- Discipline: ${subject.traitRatings.discipline}`,
          `- Drive: ${subject.traitRatings.drive}`,
          `- Empathy: ${subject.traitRatings.empathy}`,
          `- Instability: ${subject.traitRatings.instability}`,
          `- Sincerity: ${subject.traitRatings.sincerity}`,
        ].join("\n")
      : "",
  ]
    .filter(Boolean)
    .join("\n");
}

function formatPairSubject(pairSubject, fallbackSpeakerKey) {
  if (!pairSubject || !pairSubject.subject) {
    return [
      `Speaker Key: ${pairSubject?.speakerKey || fallbackSpeakerKey}`,
      formatSubject(null),
    ].join("\n");
  }

  return [
    `Speaker Key: ${pairSubject.speakerKey || fallbackSpeakerKey}`,
    formatSubject(pairSubject.subject),
  ].join("\n");
}

function buildPairsText(pairs, assignments) {
  if (!pairs || pairs.length === 0) {
    return buildAssignmentsText(assignments);
  }

  return pairs
    .map((pair, index) => {
      const subjects = Array.isArray(pair.subjects) ? pair.subjects : [];

      return [
        `Pair ${index + 1}`,
        `Pair Key: ${pair.pairKey || `pair-${index + 1}`}`,
        `Position: ${pair.position || ""}`,
        "",
        "Coworker A:",
        formatPairSubject(subjects[0], `${pair.pairKey || `pair-${index + 1}`}:a`),
        "",
        "Coworker B:",
        formatPairSubject(subjects[1], `${pair.pairKey || `pair-${index + 1}`}:b`),
      ].join("\n");
    })
    .join("\n\n");
}

app.post("/api/simulate", async (req, res) => {
  try {
    const { pairs, assignments } = req.body;

    const template = loadPromptTemplate();
    const pairsText = buildPairsText(pairs, assignments);
    const finalPrompt = template
      .replace("{{PAIRS}}", pairsText)
      .replace("{{ASSIGNMENTS}}", pairsText);

    console.log(
      `[CafeBackend] Sending OpenAI request. model=${openaiModel}, timeoutMs=${openaiTimeoutMs}, promptChars=${finalPrompt.length}`
    );

    const startedAt = Date.now();

    const response = await openai.responses.create({
      model: openaiModel,
      input: finalPrompt,
      max_output_tokens: 2500,
    }, {
      timeout: openaiTimeoutMs,
    });

    console.log(`[CafeBackend] OpenAI response received in ${Date.now() - startedAt}ms`);

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
