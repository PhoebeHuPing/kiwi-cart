package nz.co.kiwicart.config;

import io.swagger.v3.oas.models.OpenAPI;
import io.swagger.v3.oas.models.info.Contact;
import io.swagger.v3.oas.models.info.Info;
import io.swagger.v3.oas.models.info.License;
import io.swagger.v3.oas.models.security.SecurityRequirement;
import io.swagger.v3.oas.models.security.SecurityScheme;
import io.swagger.v3.oas.models.Components;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class OpenApiConfig {

    @Bean
    public OpenAPI kiwiCartOpenApi() {
        return new OpenAPI()
                .info(new Info()
                        .title("KiwiCart API")
                        .description("NZ Supermarket Price Comparison API - Compare prices across Pak'nSave, New World, and Woolworths")
                        .version("1.0.0")
                        .contact(new Contact()
                                .name("KiwiCart Team")
                                .url("https://kiwicart.azurewebsites.net"))
                        .license(new License()
                                .name("ISC")))
                .addSecurityItem(new SecurityRequirement().addList("Bearer"))
                .components(new Components()
                        .addSecuritySchemes("Bearer", new SecurityScheme()
                                .type(SecurityScheme.Type.HTTP)
                                .scheme("bearer")
                                .bearerFormat("JWT")
                                .description("Auth0 JWT token")));
    }
}
