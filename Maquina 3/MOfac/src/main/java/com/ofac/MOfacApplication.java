package com.ofac;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.scheduling.annotation.EnableScheduling;

@SpringBootApplication
@EnableScheduling
public class MOfacApplication {

	public static void main(String[] args) {
		SpringApplication.run(MOfacApplication.class, args);
	}

}
