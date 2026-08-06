package nz.co.kiwicart.controller;

import nz.co.kiwicart.entity.Feedback;
import nz.co.kiwicart.repository.FeedbackRepository;
import org.springframework.http.ResponseEntity;
import org.springframework.security.core.annotation.AuthenticationPrincipal;
import org.springframework.security.oauth2.jwt.Jwt;
import org.springframework.web.bind.annotation.*;

import java.util.List;
import java.util.Map;

@RestController
@RequestMapping("/api/v1/feedback")
public class FeedbackController {

    private final FeedbackRepository feedbackRepository;

    public FeedbackController(FeedbackRepository feedbackRepository) {
        this.feedbackRepository = feedbackRepository;
    }

    @GetMapping
    public ResponseEntity<List<Feedback>> getAllFeedback() {
        return ResponseEntity.ok(feedbackRepository.findAllByOrderByCreatedAtDesc());
    }

    @PostMapping
    public ResponseEntity<Feedback> submitFeedback(
            @AuthenticationPrincipal Jwt jwt,
            @RequestBody Map<String, String> body) {

        String message = body.get("message");
        if (message == null || message.isBlank()) {
            throw new IllegalArgumentException("Message is required");
        }

        String userId = jwt.getSubject();
        String userName = jwt.getClaimAsString("name");
        String category = body.getOrDefault("category", "general");

        Feedback feedback = Feedback.builder()
                .userId(userId)
                .userName(userName != null ? userName : "Anonymous")
                .message(message.trim())
                .category(category)
                .build();

        Feedback saved = feedbackRepository.save(feedback);
        return ResponseEntity.ok(saved);
    }
}
