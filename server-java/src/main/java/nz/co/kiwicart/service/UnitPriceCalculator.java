package nz.co.kiwicart.service;

import org.springframework.stereotype.Service;

import java.util.regex.Matcher;
import java.util.regex.Pattern;

/**
 * Calculates standardized unit prices ($/L, $/100g, $/each)
 * by parsing product names for volume/weight information.
 *
 * Handles multipack formats like "6 x 330ml" by calculating total volume/weight
 * and returning per-unit-of-measure price (e.g. $/L) rather than per-item.
 */
@Service
public class UnitPriceCalculator {

    // Multipack with volume: "6 x 330ml", "4x500mL", "12 x 1L"
    private static final Pattern MULTIPACK_ML = Pattern.compile("(\\d+)\\s*[xX]\\s*(\\d+(?:\\.\\d+)?)\\s*[mM][lL]\\b");
    private static final Pattern MULTIPACK_L = Pattern.compile("(\\d+)\\s*[xX]\\s*(\\d+(?:\\.\\d+)?)\\s*[lL](?:itre)?s?\\b");

    // Multipack with weight: "6 x 100g", "4x250g", "3 x 1kg"
    private static final Pattern MULTIPACK_G = Pattern.compile("(\\d+)\\s*[xX]\\s*(\\d+(?:\\.\\d+)?)\\s*[gG](?:[mM])?\\b");
    private static final Pattern MULTIPACK_KG = Pattern.compile("(\\d+)\\s*[xX]\\s*(\\d+(?:\\.\\d+)?)\\s*[kK][gG]\\b");

    // Single volume: "2L", "750ml", "1.5l", "500mL"
    private static final Pattern VOLUME_LITRES = Pattern.compile("(\\d+(?:\\.\\d+)?)\\s*[lL](?:itre)?s?\\b");
    private static final Pattern VOLUME_ML = Pattern.compile("(\\d+(?:\\.\\d+)?)\\s*[mM][lL]\\b");

    // Single weight: "500g", "1.5kg", "100gm"
    private static final Pattern WEIGHT_KG = Pattern.compile("(\\d+(?:\\.\\d+)?)\\s*[kK][gG]\\b");
    private static final Pattern WEIGHT_G = Pattern.compile("(\\d+(?:\\.\\d+)?)\\s*[gG](?:[mM])?\\b");

    // Pack counts (no unit): "6 Pack", "12pk"
    private static final Pattern PACK_COUNT = Pattern.compile("(\\d+)\\s*(?:[pP]ack|[pP][kK])\\b");

    public String calculate(String productName, double price) {
        if (productName == null || productName.isEmpty() || price <= 0) {
            return null;
        }

        // 1. Try multipack with volume (highest priority — "6 x 330ml" → total litres)
        Double multipackLitres = extractMultipackLitres(productName);
        if (multipackLitres != null && multipackLitres > 0) {
            double perLitre = price / multipackLitres;
            return String.format("$%.2f/L", perLitre);
        }

        // 2. Try multipack with weight ("6 x 100g" → total grams)
        Double multipackGrams = extractMultipackGrams(productName);
        if (multipackGrams != null && multipackGrams > 0) {
            double per100g = (price / multipackGrams) * 100;
            return String.format("$%.2f/100g", per100g);
        }

        // 3. Try single volume (litres)
        Double litres = extractLitres(productName);
        if (litres != null && litres > 0) {
            double perLitre = price / litres;
            return String.format("$%.2f/L", perLitre);
        }

        // 4. Try single weight (grams)
        Double grams = extractGrams(productName);
        if (grams != null && grams > 0) {
            double per100g = (price / grams) * 100;
            return String.format("$%.2f/100g", per100g);
        }

        // 5. Try plain pack count (no unit — "6 Pack" → per each)
        Integer count = extractPackCount(productName);
        if (count != null && count > 1) {
            double perUnit = price / count;
            return String.format("$%.2f/each", perUnit);
        }

        return null;
    }

    private Double extractMultipackLitres(String name) {
        Matcher m = MULTIPACK_ML.matcher(name);
        if (m.find()) {
            int count = Integer.parseInt(m.group(1));
            double mlEach = Double.parseDouble(m.group(2));
            return (count * mlEach) / 1000.0;
        }

        m = MULTIPACK_L.matcher(name);
        if (m.find()) {
            int count = Integer.parseInt(m.group(1));
            double litresEach = Double.parseDouble(m.group(2));
            return count * litresEach;
        }

        return null;
    }

    private Double extractMultipackGrams(String name) {
        Matcher m = MULTIPACK_KG.matcher(name);
        if (m.find()) {
            int count = Integer.parseInt(m.group(1));
            double kgEach = Double.parseDouble(m.group(2));
            return count * kgEach * 1000;
        }

        m = MULTIPACK_G.matcher(name);
        if (m.find()) {
            int count = Integer.parseInt(m.group(1));
            double gEach = Double.parseDouble(m.group(2));
            return count * gEach;
        }

        return null;
    }

    private Double extractLitres(String name) {
        Matcher m = VOLUME_LITRES.matcher(name);
        if (m.find()) {
            return Double.parseDouble(m.group(1));
        }

        m = VOLUME_ML.matcher(name);
        if (m.find()) {
            return Double.parseDouble(m.group(1)) / 1000.0;
        }

        return null;
    }

    private Double extractGrams(String name) {
        Matcher m = WEIGHT_KG.matcher(name);
        if (m.find()) {
            return Double.parseDouble(m.group(1)) * 1000;
        }

        m = WEIGHT_G.matcher(name);
        if (m.find()) {
            return Double.parseDouble(m.group(1));
        }

        return null;
    }

    private Integer extractPackCount(String name) {
        Matcher m = PACK_COUNT.matcher(name);
        if (m.find()) {
            return Integer.parseInt(m.group(1));
        }

        return null;
    }
}
