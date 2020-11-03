-- phpMyAdmin SQL Dump
-- version 5.0.2
-- https://www.phpmyadmin.net/
--
-- Host: localhost
-- Erstellungszeit: 02. Nov 2020 um 21:52
-- Server-Version: 10.3.15-MariaDB
-- PHP-Version: 7.4.11

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
START TRANSACTION;
SET time_zone = "+00:00";


/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8mb4 */;

--
-- Datenbank: `twitch_clip_archive`
--

-- --------------------------------------------------------

--
-- Tabellenstruktur für Tabelle `clips`
--

CREATE TABLE `clips` (
  `id` text CHARACTER SET utf8 COLLATE utf8_german2_ci NOT NULL,
  `url` text CHARACTER SET utf8 COLLATE utf8_german2_ci NOT NULL,
  `embed_url` text CHARACTER SET utf8 COLLATE utf8_german2_ci NOT NULL,
  `broadcaster_id` int(11) NOT NULL,
  `broadcaster_name` text CHARACTER SET utf8 COLLATE utf8_german2_ci NOT NULL,
  `creator_id` int(11) NOT NULL,
  `creator_name` text CHARACTER SET utf8 COLLATE utf8_german2_ci NOT NULL,
  `video_id` int(11) NOT NULL,
  `game_id` int(11) NOT NULL,
  `language` text CHARACTER SET utf8 COLLATE utf8_german2_ci NOT NULL,
  `title` text CHARACTER SET utf8 COLLATE utf8_german2_ci NOT NULL,
  `view_count` int(11) NOT NULL,
  `created_at` date NOT NULL,
  `thumbnail_url` text CHARACTER SET utf8 COLLATE utf8_german2_ci NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8;
COMMIT;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
