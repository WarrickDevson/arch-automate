<template>
  <div class="relative min-h-screen overflow-hidden bg-bg">
    <div class="pointer-events-none absolute inset-0 opacity-85">
      <div class="absolute -left-16 top-6 h-72 w-72 rounded-full bg-cyan-300/20 blur-3xl" />
      <div class="absolute right-0 top-20 h-96 w-96 rounded-full bg-teal-400/15 blur-3xl" />
      <div
        class="absolute -bottom-28 left-1/3 h-[28rem] w-[28rem] rounded-full bg-lime-300/10 blur-3xl"
      />
      <div
        class="absolute inset-0 bg-[radial-gradient(circle_at_18%_18%,rgba(14,69,83,0.1),transparent_34%),radial-gradient(circle_at_82%_75%,rgba(220,229,87,0.12),transparent_36%)]"
      />
    </div>

    <main class="relative z-10 min-h-screen">
      <section class="min-h-screen w-full">
        <div class="mx-auto h-full w-full max-w-7xl px-4 sm:px-8 lg:px-12">
          <RouterView />
        </div>
      </section>
    </main>
  </div>
</template>
