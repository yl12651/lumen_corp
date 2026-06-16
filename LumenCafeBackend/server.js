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
const useDebugSimulationResponse =
  process.env.CAFE_DEBUG_SIMULATION_RESPONSE === "true";
const useDebugRetentionReviewResponse =
  process.env.CAFE_DEBUG_RETENTION_REVIEW_RESPONSE === "true";

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

function loadRetentionReviewPromptTemplate() {
  const promptPath = path.join(__dirname, "prompts", "retention_review_prompt.md");
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

function getPairSubject(pair, subjectIndex) {
  const subjects = Array.isArray(pair?.subjects) ? pair.subjects : [];
  return subjects[subjectIndex] || null;
}

function getSubjectId(pairSubject, fallbackId) {
  return pairSubject?.subject?.id || fallbackId;
}

function getSubjectType(pairSubject) {
  return pairSubject?.subject?.type || "Unassigned Subject";
}

function getSpeakerKey(pairSubject, fallbackSpeakerKey) {
  return pairSubject?.speakerKey || fallbackSpeakerKey;
}

function buildSelectedSubject(pairSubject, fallbackSpeakerKey, position, fallbackId) {
  return {
    speakerKey: getSpeakerKey(pairSubject, fallbackSpeakerKey),
    position,
    id: getSubjectId(pairSubject, fallbackId),
    type: getSubjectType(pairSubject),
  };
}

function buildDebugConversation(pair, index) {
  const pairKey = pair?.pairKey || `pair-${index + 1}`;
  const position = pair?.position || `Position ${index + 1}`;
  const firstSubject = getPairSubject(pair, 0);
  const secondSubject = getPairSubject(pair, 1);
  const firstSpeakerKey = getSpeakerKey(firstSubject, `${pairKey}:a`);
  const secondSpeakerKey = getSpeakerKey(secondSubject, `${pairKey}:b`);
  const firstId = getSubjectId(firstSubject, "Subject A");
  const secondId = getSubjectId(secondSubject, "Subject B");
  const firstType = getSubjectType(firstSubject);
  const secondType = getSubjectType(secondSubject);

  return {
    pairKey,
    position,
    selectedPair: [
      buildSelectedSubject(firstSubject, firstSpeakerKey, position, firstId),
      buildSelectedSubject(secondSubject, secondSpeakerKey, position, secondId),
    ],
    sceneTitle: `Debug Shift at ${position}`,
    context:
      `Debug response: ${firstId} and ${secondId} are working ${position}. ` +
      "This conversation was generated locally by the backend test path.",
    bubbles: [
      {
        speakerKey: firstSpeakerKey,
        speakerId: firstId,
        position,
        text: `I am ${firstId}, a ${firstType}, and this is a fast debug conversation for ${position}.`,
      },
      {
        speakerKey: secondSpeakerKey,
        speakerId: secondId,
        position,
        text: `I am ${secondId}, a ${secondType}. No OpenAI request was made for this one.`,
      },
      {
        speakerKey: firstSpeakerKey,
        speakerId: firstId,
        position,
        text: "Good. That means the Unity ending flow can be tested without waiting.",
      },
      {
        speakerKey: secondSpeakerKey,
        speakerId: secondId,
        position,
        text: "After this ends, our pair should stop jumping while the unread pairs keep moving.",
      },
    ],
  };
}

function buildDebugSimulationResponse(pairs) {
  const sourcePairs = Array.isArray(pairs) && pairs.length > 0
    ? pairs
    : [
        { pairKey: "counter", position: "Counter" },
        { pairKey: "bar", position: "Barista" },
        { pairKey: "floor", position: "Floor" },
      ];

  return {
    conversations: sourcePairs.map(buildDebugConversation),
  };
}

function normalizeRetentionSelections(selections) {
  return Array.isArray(selections) ? selections : [];
}

function formatRetentionSelection(selection, index) {
  const subject = selection?.subject;
  const positionName = selection?.positionName || selection?.position || "";
  const shouldRemain = Boolean(selection?.shouldRemain);

  return [
    `Selection ${index + 1}`,
    `Pair Key: ${selection?.pairKey || ""}`,
    `Position: ${positionName}`,
    `Slot: ${selection?.slot || ""}`,
    `Speaker Key: ${selection?.speakerKey || ""}`,
    `Player Decision: ${shouldRemain ? "KEEP" : "REMOVE"}`,
    formatSubject(subject),
  ].join("\n");
}

function buildRetentionSelectionsText(selections) {
  const normalizedSelections = normalizeRetentionSelections(selections);

  if (normalizedSelections.length === 0) {
    return "No retention selections were submitted.";
  }

  return normalizedSelections
    .map(formatRetentionSelection)
    .join("\n\n");
}

function getSelectedRetentionSubjects(selections, shouldRemain) {
  return normalizeRetentionSelections(selections)
    .filter((selection) => Boolean(selection?.shouldRemain) === shouldRemain);
}

function buildDebugRetentionReviewResponse(selections) {
  const keptSelections = getSelectedRetentionSubjects(selections, true);
  const removedSelections = getSelectedRetentionSubjects(selections, false);
  const firstKept = keptSelections[0];
  const firstKeptType = firstKept?.subject?.type || "a mysteriously undocumented subject";
  const firstKeptPosition = firstKept?.positionName || firstKept?.position || "an undefined position";

  return {
    title: "Retention Review",
    lines: [
      {
        speakerName: "Clothos",
        text: "Thank you. I have reviewed your retention proposal with the warmth and flexibility expected from Human Resources.",
        live2DTriggerName: "Speak",
      },
      {
        speakerName: "Clothos",
        text: `You selected ${keptSelections.length} subject(s) to remain and ${removedSelections.length} subject(s) to quietly become someone else's problem.`,
        live2DTriggerName: "Speak",
      },
      {
        speakerName: "Clothos",
        text: `For example, keeping ${firstKeptType} at ${firstKeptPosition} is a decision. Not necessarily a good decision, but certainly one with paperwork attached.`,
        live2DTriggerName: "Speak",
      },
      {
        speakerName: "Clothos",
        text: "This is a debug retention review. No OpenAI request was made, so please imagine the institutional bias at full resolution.",
        live2DTriggerName: "Speak",
      },
    ],
  };
}

app.post("/api/simulate", async (req, res) => {
  try {
    const { pairs, assignments } = req.body;

    if (useDebugSimulationResponse) {
      console.log("[CafeBackend] Returning debug simulation response. OpenAI request skipped.");
      res.json({
        text: JSON.stringify(buildDebugSimulationResponse(pairs), null, 2),
      });
      return;
    }

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

app.post("/api/retention-review", async (req, res) => {
  try {
    const { selections } = req.body;

    if (useDebugRetentionReviewResponse) {
      console.log("[CafeBackend] Returning debug retention review response. OpenAI request skipped.");
      res.json({
        text: JSON.stringify(buildDebugRetentionReviewResponse(selections), null, 2),
      });
      return;
    }

    const template = loadRetentionReviewPromptTemplate();
    const selectionsText = buildRetentionSelectionsText(selections);
    const finalPrompt = template.replace("{{SELECTIONS}}", selectionsText);

    console.log(
      `[CafeBackend] Sending retention review OpenAI request. model=${openaiModel}, timeoutMs=${openaiTimeoutMs}, promptChars=${finalPrompt.length}`
    );

    const startedAt = Date.now();

    const response = await openai.responses.create({
      model: openaiModel,
      input: finalPrompt,
      max_output_tokens: 1600,
    }, {
      timeout: openaiTimeoutMs,
    });

    console.log(`[CafeBackend] Retention review response received in ${Date.now() - startedAt}ms`);

    res.json({
      text: response.output_text,
    });
  } catch (error) {
    console.error(error);
    res.status(500).json({
      error: "Failed to generate retention review.",
    });
  }
});

const port = process.env.PORT || 3000;
app.listen(port, () => {
  console.log(`Lumen Cafe backend running on http://localhost:${port}`);
});
