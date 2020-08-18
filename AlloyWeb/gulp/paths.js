'use strict';

module.exports = {
  source: {
    templates: './src/templates/**/*.jade',
    slides: './src/slides/*.md',
    js: './src/js/**/*.js',
    styl: './src/styl/**/*.styl',
    img: './src/img/**/*',
    files: {
      jade: './src/templates/index.jade',
      styl: './src/styl/main.styl'
    }
  },

  browserSync: {
    html: './presentation/**/*.html',
    css: './presentation/css/**/*.css',
    js: './presentation/js/**/*.js',
    img: './presentation/img/**/*'
  },

  build: {
    html: './presentation/',
    css: './presentation/css',
    js: './presentation/js',
    img: './presentation/img',
  },

  deploy: {
    pages: './presentation/**/*'
  }
};
