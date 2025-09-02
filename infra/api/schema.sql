CREATE TABLE IF NOT EXISTS scores (
	id bigserial primary key,
	player_name text not null,
	level int not null,
	score int not null,
	percent_of_optimal numeric(5,2) not null,
	budget int not null,
	created_at timestamptz default now(),
	meta jsonb
);
