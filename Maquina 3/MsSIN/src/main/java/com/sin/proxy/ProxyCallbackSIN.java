package com.sin.proxy;

import com.sin.dto.CallbackDTO;
import io.netty.channel.ChannelOption;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.http.client.reactive.ReactorClientHttpConnector;
import org.springframework.stereotype.Component;
import org.springframework.web.reactive.function.client.WebClient;
import reactor.core.publisher.Mono;
import reactor.netty.http.client.HttpClient;

import java.time.Duration;

@Component
public class ProxyCallbackSIN {

    private final WebClient webClient;
    private final String callbackUrl;

    public ProxyCallbackSIN(WebClient.Builder builder,
                            @Value("${sso.callback.url}") String callbackUrl,
                            @Value("${sso.callback.timeout:5000}") long timeoutMs) {
        HttpClient httpClient = HttpClient.create()
                .option(ChannelOption.CONNECT_TIMEOUT_MILLIS, (int) timeoutMs)
                .responseTimeout(Duration.ofMillis(timeoutMs));
        this.webClient = builder
                .clientConnector(new ReactorClientHttpConnector(httpClient))
                .build();
        this.callbackUrl = callbackUrl;
    }

    public Mono<String> notificar(CallbackDTO dto) {
        return webClient.post()
                .uri(callbackUrl)
                .bodyValue(dto)
                .retrieve()
                .bodyToMono(String.class)
                .timeout(Duration.ofMillis(5000));
    }
}