import pytest
from pydantic import ValidationError

from app.schemas import RankRequest
from app.services.recommender import rank_candidates


def payload(candidates):
    return {"analysis": {"mood":"stressed", "energy":0.2, "valence":-0.3, "themes":["relax"], "confidence":0.7},
            "preferences":{"categories":["music"], "tags":["relax"], "seenIds":[]}, "candidates":candidates}


def test_catalog_and_top_five(catalog):
    assert len(catalog) >= 30
    assert {item["category"] for item in catalog} == {"movie", "music", "book", "activity"}
    request = RankRequest.model_validate(payload(catalog))
    result = rank_candidates(request)
    assert len(result) == 5
    assert result[0].id == "music-01"
    assert [item.score for item in result] == sorted([item.score for item in result], reverse=True)
    for item in result:
        c = item.components
        assert item.score == pytest.approx(0.4*c.mood_match + 0.3*c.preference_match + 0.2*c.context_match + 0.1*c.novelty, abs=1e-8)


def test_exact_score_and_novelty():
    candidate = {"id":"one", "title":"Test", "category":"music", "tags":["relax"], "moods":["stressed"], "energy":0.2}
    request = RankRequest.model_validate(payload([candidate]))
    assert rank_candidates(request)[0].score == 1.0
    request.preferences.seen_ids = ["one"]
    assert rank_candidates(request)[0].score == 0.9


def test_deterministic_ties_independent_of_input_order():
    candidate = {"id":"b", "title":"Test", "category":"music", "tags":["relax"], "moods":["stressed"], "energy":0.2}
    candidates = [candidate, candidate | {"id":"a"}]
    forward = rank_candidates(RankRequest.model_validate(payload(candidates)))
    reverse = rank_candidates(RankRequest.model_validate(payload(candidates[::-1])))
    assert forward == reverse
    assert [row.id for row in forward] == ["a", "b"]


def test_no_preferences_and_empty_catalog(catalog):
    body = payload(catalog)
    body["preferences"] = {}
    body["analysis"]["themes"] = []
    result = rank_candidates(RankRequest.model_validate(body))
    assert all(row.components.preference_match == 0.5 for row in result)
    body["candidates"] = []
    assert rank_candidates(RankRequest.model_validate(body)) == []


def test_repeated_catalog_ranking(catalog):
    request = RankRequest.model_validate(payload(catalog))
    assert rank_candidates(request) == rank_candidates(request)


def test_duplicate_ids_rejected(catalog):
    with pytest.raises(ValidationError):
        RankRequest.model_validate(payload([catalog[0], catalog[0]]))
