const { defineConfig } = require("cypress");

module.exports = defineConfig({
  e2e: {
    baseUrl: "https://projekt-tripfy.pl", // URL frontendu (produkcyjny lub stagingowy)
    viewportWidth: 1920,
    viewportHeight: 1080,
    
    env: {
      backendUrl: "https://projekt-tripify.hostingasp.pl/", // URL backendu (zmień na swój)
    },

    setupNodeEvents(on, config) {
      // Zezwolenie na testy między różnymi domenami (CORS)
      on("task", {
        log(message) {
          console.log(message);
          return null;
        },
      });

      return config;
    }
  }
});
