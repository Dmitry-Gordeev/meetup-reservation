-- WP-0.3.2: organizer_profiles, participant_profiles

CREATE TABLE IF NOT EXISTS meetup.organizer_profiles (
    user_id             BIGINT NOT NULL PRIMARY KEY REFERENCES meetup.users(id) ON DELETE CASCADE,
    name                VARCHAR(255) NOT NULL,
    description         TEXT,
    avatar_content      BYTEA,
    avatar_content_type VARCHAR(100),
    avatar_file_name    VARCHAR(255)
);

CREATE TABLE IF NOT EXISTS meetup.participant_profiles (
    user_id     BIGINT NOT NULL PRIMARY KEY REFERENCES meetup.users(id) ON DELETE CASCADE,
    first_name  VARCHAR(100) NOT NULL,
    last_name   VARCHAR(100) NOT NULL,
    middle_name VARCHAR(100),
    email       VARCHAR(255) NOT NULL,
    phone       VARCHAR(50)
);
