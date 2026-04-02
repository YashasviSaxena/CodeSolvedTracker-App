namespace CodeSolvedTracker.Services
{
    public class AIService
    {
        public List<string> GetWeakTopics(List<ProblemData> problems)
        {
            var topicPerformance = new Dictionary<string, (int Solved, int Total)>();

            foreach (var problem in problems)
            {
                if (!topicPerformance.ContainsKey(problem.Topic))
                    topicPerformance[problem.Topic] = (0, 0);

                var (solved, total) = topicPerformance[problem.Topic];
                topicPerformance[problem.Topic] = (solved + (problem.IsSolved ? 1 : 0), total + 1);
            }

            var weakTopics = topicPerformance
                .Where(t => t.Value.Solved / (float)Math.Max(t.Value.Total, 1) < 0.5f)
                .Select(t => t.Key)
                .ToList();

            return weakTopics.Any() ? weakTopics : new List<string> { "Arrays", "Strings" };
        }

        public DifficultyPrediction GetRecommendedDifficulty(List<ProblemData> solvedProblems)
        {
            if (!solvedProblems.Any())
            {
                return new DifficultyPrediction { RecommendedDifficulty = "Easy", Confidence = 0.8f };
            }

            var easyCount = solvedProblems.Count(p => p.Difficulty == "Easy" && p.IsSolved);
            var mediumCount = solvedProblems.Count(p => p.Difficulty == "Medium" && p.IsSolved);
            var hardCount = solvedProblems.Count(p => p.Difficulty == "Hard" && p.IsSolved);
            var totalAttempted = solvedProblems.Count;

            if (totalAttempted == 0) return new DifficultyPrediction { RecommendedDifficulty = "Easy", Confidence = 0.8f };

            var easySuccessRate = easyCount / (float)totalAttempted;
            var mediumSuccessRate = mediumCount / (float)totalAttempted;
            var hardSuccessRate = hardCount / (float)totalAttempted;

            if (easySuccessRate > 0.7f && mediumSuccessRate < 0.5f)
                return new DifficultyPrediction { RecommendedDifficulty = "Medium", Confidence = 0.7f };
            else if (mediumSuccessRate > 0.6f && hardSuccessRate < 0.4f)
                return new DifficultyPrediction { RecommendedDifficulty = "Hard", Confidence = 0.6f };
            else if (easySuccessRate < 0.5f)
                return new DifficultyPrediction { RecommendedDifficulty = "Easy", Confidence = 0.8f };
            else
                return new DifficultyPrediction { RecommendedDifficulty = "Medium", Confidence = 0.6f };
        }

        public SkillGapPrediction PredictSkillGap(ProblemData problem)
        {
            return new SkillGapPrediction
            {
                WillStruggle = !problem.IsSolved,
                Probability = problem.IsSolved ? 0.3f : 0.7f,
                SuggestedTopic = problem.Topic
            };
        }
    }

    public class ProblemData
    {
        public string Platform { get; set; } = string.Empty;
        public string Difficulty { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public bool IsSolved { get; set; }
        public float TimeSpentMinutes { get; set; }
    }

    public class SkillGapPrediction
    {
        public bool WillStruggle { get; set; }
        public float Probability { get; set; }
        public float Score { get; set; }
        public string SuggestedTopic { get; set; } = string.Empty;
    }

    public class DifficultyPrediction
    {
        public string RecommendedDifficulty { get; set; } = string.Empty;
        public float Confidence { get; set; }
    }
}