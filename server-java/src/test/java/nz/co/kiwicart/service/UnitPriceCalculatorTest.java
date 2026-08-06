package nz.co.kiwicart.service;

import org.junit.jupiter.api.Test;

import static org.assertj.core.api.Assertions.assertThat;

class UnitPriceCalculatorTest {

    private final UnitPriceCalculator calculator = new UnitPriceCalculator();

    // --- Multipack with volume ---

    @Test
    void calculate_multipack_ml() {
        // 6 x 330ml = 1.98L, $12.00 → $6.06/L
        assertThat(calculator.calculate("Coca Cola 6 x 330ml", 12.00)).isEqualTo("$6.06/L");
        // 4x500mL = 2L, $8.00 → $4.00/L
        assertThat(calculator.calculate("Sparkling Water 4x500mL", 8.00)).isEqualTo("$4.00/L");
    }

    @Test
    void calculate_multipack_litres() {
        // 12 x 1L = 12L, $24.00 → $2.00/L
        assertThat(calculator.calculate("Juice 12 x 1L", 24.00)).isEqualTo("$2.00/L");
    }

    @Test
    void calculate_multipack_grams() {
        // 6 x 100g = 600g, $9.00 → $1.50/100g
        assertThat(calculator.calculate("Snack Bars 6 x 100g", 9.00)).isEqualTo("$1.50/100g");
    }

    @Test
    void calculate_multipack_kg() {
        // 3 x 1kg = 3000g, $15.00 → $0.50/100g
        assertThat(calculator.calculate("Rice 3 x 1kg", 15.00)).isEqualTo("$0.50/100g");
    }

    // --- Single volume ---

    @Test
    void calculate_withLitres() {
        assertThat(calculator.calculate("Anchor Milk 2L", 4.88)).isEqualTo("$2.44/L");
        assertThat(calculator.calculate("Juice 1.5L", 3.99)).isEqualTo("$2.66/L");
    }

    @Test
    void calculate_withMillilitres() {
        assertThat(calculator.calculate("Sauce 500ml", 3.50)).isEqualTo("$7.00/L");
        assertThat(calculator.calculate("Yoghurt 750mL", 4.50)).isEqualTo("$6.00/L");
    }

    // --- Single weight ---

    @Test
    void calculate_withKilograms() {
        assertThat(calculator.calculate("Rice 1kg", 3.00)).isEqualTo("$0.30/100g");
        assertThat(calculator.calculate("Flour 1.5kg", 4.50)).isEqualTo("$0.30/100g");
    }

    @Test
    void calculate_withGrams() {
        assertThat(calculator.calculate("Cheese 500g", 8.00)).isEqualTo("$1.60/100g");
        assertThat(calculator.calculate("Butter 250g", 4.00)).isEqualTo("$1.60/100g");
    }

    // --- Plain pack count ---

    @Test
    void calculate_withPackCount() {
        assertThat(calculator.calculate("Eggs 12 Pack", 6.00)).isEqualTo("$0.50/each");
        assertThat(calculator.calculate("Beer 6pk", 12.00)).isEqualTo("$2.00/each");
    }

    // --- Edge cases ---

    @Test
    void calculate_withNoUnitInfo_returnsNull() {
        assertThat(calculator.calculate("Banana", 2.50)).isNull();
        assertThat(calculator.calculate("Bread", 3.99)).isNull();
    }

    @Test
    void calculate_withNullOrEmpty_returnsNull() {
        assertThat(calculator.calculate(null, 1.0)).isNull();
        assertThat(calculator.calculate("", 1.0)).isNull();
        assertThat(calculator.calculate("Milk 2L", 0)).isNull();
    }

    @Test
    void calculate_multipackTakesPriorityOverSingleVolume() {
        // "6 x 330ml" should be parsed as multipack (1.98L), not as "330ml" single
        String result = calculator.calculate("Coke 6 x 330ml Cans", 12.00);
        assertThat(result).isEqualTo("$6.06/L");
    }
}
