from app.schemas import RankedItem, RankRequest, ScoreComponents


def canonical(values: list[str]) -> set[str]:
    return {value.strip().casefold() for value in values if value.strip()}


def rank_candidates(request: RankRequest) -> list[RankedItem]:
    analysis, preferences = request.analysis, request.preferences
    wanted_tags, themes = canonical(preferences.tags), canonical(analysis.themes)
    seen = set(preferences.seen_ids)
    ranked = []
    for item in request.candidates:
        tags = canonical(item.tags)
        preference_parts = []
        if preferences.categories:
            preference_parts.append(float(item.category in preferences.categories))
        if wanted_tags:
            preference_parts.append(len(tags & wanted_tags) / len(wanted_tags))
        preference_match = sum(preference_parts) / len(preference_parts) if preference_parts else 0.5
        theme_match = len(tags & themes) / len(themes) if themes else 0.5
        energy_match = 1 - abs(item.energy - analysis.energy)
        components = ScoreComponents(
            mood_match=float(analysis.mood in item.moods), preference_match=preference_match,
            context_match=(energy_match + theme_match) / 2, novelty=float(item.id not in seen),
        )
        score = (0.4 * components.mood_match + 0.3 * components.preference_match
                 + 0.2 * components.context_match + 0.1 * components.novelty)
        ranked.append(RankedItem(
            id=item.id, score=round(score, 8), components=components,
            explanation=(f"Настроение: {components.mood_match:.2f}; предпочтения: {preference_match:.2f}; "
                         f"контекст: {components.context_match:.2f}; новизна: {components.novelty:.2f}."),
        ))
    return sorted(ranked, key=lambda item: (-item.score, item.id))[:5]
