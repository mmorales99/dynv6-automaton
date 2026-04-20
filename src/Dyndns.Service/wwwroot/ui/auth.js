function setFeedback(feedback, message, isError = false) {
  feedback.textContent = message;
  feedback.classList.toggle("error", isError);
}

document.addEventListener("DOMContentLoaded", () => {
  const loginForm = document.getElementById("loginForm");
  const setupForm = document.getElementById("setupForm");

  if (loginForm) {
    const feedback = document.getElementById("feedback");
    const passwordInput = document.getElementById("password");

    loginForm.addEventListener("submit", async (event) => {
      event.preventDefault();
      setFeedback(feedback, "Signing in...");

      try {
        const response = await fetch("/api/login", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ password: passwordInput.value }),
        });

        if (!response.ok) {
          const message = await response.text();
          throw new Error(message || `Login failed with ${response.status}`);
        }

        window.location.href = "/dashboard";
      } catch (error) {
        setFeedback(feedback, error.message, true);
      }
    });
  }

  if (setupForm) {
    const feedback = document.getElementById("feedback");
    const adminPasswordInput = document.getElementById("adminPassword");
    const viewerPasswordInput = document.getElementById("viewerPassword");

    function validatePasswords() {
      if (
        adminPasswordInput.value &&
        viewerPasswordInput.value &&
        adminPasswordInput.value === viewerPasswordInput.value
      ) {
        setFeedback(
          feedback,
          "Viewer and admin passwords must be different.",
          true,
        );
        return false;
      }

      setFeedback(feedback, "");
      return true;
    }

    adminPasswordInput.addEventListener("input", validatePasswords);
    viewerPasswordInput.addEventListener("input", validatePasswords);

    setupForm.addEventListener("submit", async (event) => {
      event.preventDefault();

      if (!validatePasswords()) {
        return;
      }

      setFeedback(feedback, "Creating users...");

      try {
        const response = await fetch("/api/setup", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            adminPassword: adminPasswordInput.value,
            viewerPassword: viewerPasswordInput.value,
          }),
        });

        if (!response.ok) {
          const message = await response.text();
          throw new Error(message || `Setup failed with ${response.status}`);
        }

        window.location.href = "/";
      } catch (error) {
        setFeedback(feedback, error.message, true);
      }
    });
  }
});
